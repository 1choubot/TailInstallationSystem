using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public class DataManager : IDisposable
    {
        private readonly CommunicationConfig _config;
        private readonly HttpClient httpClient;
        private readonly DataService _dataService;
        private System.Threading.Timer retryTimer;
        private System.Threading.Timer cleanupTimer;
        private const int MAX_RETRY_COUNT = 5;
        private bool _disposed = false;

        public DataManager(CommunicationConfig config = null)
        {
            _config = config ?? ConfigManager.LoadConfig();
            _dataService = new DataService(_config);
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            retryTimer = new System.Threading.Timer(ProcessRetryQueue, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

            // 启动备份清理定时器（每天清理一次）
            cleanupTimer = new System.Threading.Timer(state => CleanupBackupDirectory(), null,
                TimeSpan.FromHours(1), TimeSpan.FromHours(24));
        }

        /// <summary>
        /// 安全的数据库操作封装
        /// </summary>
        private async Task<T> ExecuteWithContext<T>(Func<NodeInstrumentMESEntities, Task<T>> operation, T defaultValue = default(T))
        {
            if (_disposed)
            {
                LogManager.LogWarning("DataManager已释放，跳过数据库操作");
                return defaultValue;
            }

            try
            {
                using (var context = new NodeInstrumentMESEntities())
                {
                    context.Database.CommandTimeout = 30;
                    return await operation(context);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"数据库操作异常: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 保存产品数据 - 统一使用当前工序条码
        /// </summary>
        public async Task<bool> SaveProductData(string currentBarcode, string[] processDataArray,
            string tailProcessData, string completeData)
        {
            if (string.IsNullOrWhiteSpace(currentBarcode))
            {
                LogManager.LogError("产品条码不能为空");
                return false;
            }

            return await ExecuteWithContext(async context =>
            {
                LogManager.LogInfo($"开始保存当前工序数据: {currentBarcode}");

                var existingProduct = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == currentBarcode);

                if (existingProduct != null)
                {
                    LogManager.LogInfo($"更新现有产品记录: {currentBarcode}");
                    existingProduct.Process1_Data = processDataArray.Length > 0 ? processDataArray[0] : existingProduct.Process1_Data;
                    existingProduct.Process2_Data = processDataArray.Length > 1 ? processDataArray[1] : existingProduct.Process2_Data;
                    existingProduct.Process3_Data = processDataArray.Length > 2 ? processDataArray[2] : existingProduct.Process3_Data;
                    existingProduct.Process4_Data = tailProcessData;
                    existingProduct.CompleteData = completeData;
                    existingProduct.CompletedTime = DateTime.Now;
                    existingProduct.IsCompleted = true;
                    existingProduct.IsUploaded = false;
                    context.Entry(existingProduct).State = EntityState.Modified;
                }
                else
                {
                    LogManager.LogInfo($"创建新的产品记录: {currentBarcode}");
                    var product = new ProductData
                    {
                        Barcode = currentBarcode,
                        Process1_Data = processDataArray.Length > 0 ? processDataArray[0] : null,
                        Process2_Data = processDataArray.Length > 1 ? processDataArray[1] : null,
                        Process3_Data = processDataArray.Length > 2 ? processDataArray[2] : null,
                        Process4_Data = tailProcessData,
                        CompleteData = completeData,
                        CreatedTime = DateTime.Now,
                        CompletedTime = DateTime.Now,
                        IsCompleted = true,
                        IsUploaded = false
                    };
                    context.ProductData.Add(product);
                }

                await context.SaveChangesAsync();
                LogManager.LogInfo($"当前工序数据保存成功: {currentBarcode}");
                return true;
            }, false);
        }

        /// <summary>
        /// 上传数据到服务器 - 使用扫描条码作为标识
        /// </summary>
        public async Task<bool> UploadToServer(string scannedBarcode, string completeData)
        {
            var startTime = DateTime.Now;

            try
            {
                LogManager.LogInfo($"上传启动 | 条码:{scannedBarcode} | 大小:{completeData?.Length ?? 0}B | 服务器:{_config.Server.WebSocketUrl}");

                // 先保存到重试队列（确保数据不丢失）
                await AddToUploadQueue(scannedBarcode, completeData);
                LogManager.LogDebug($"队列保存 | 条码:{scannedBarcode}");

                // 立即尝试上传
                bool uploadSuccess = await TryUploadData(scannedBarcode, completeData);

                if (uploadSuccess)
                {
                    // 上传成功，从队列中移除
                    await RemoveFromUploadQueue(scannedBarcode);
                    await UpdateUploadStatus(scannedBarcode, true);

                    var duration = (DateTime.Now - startTime).TotalSeconds;
                    LogManager.LogInfo($"上传完成 | 条码:{scannedBarcode} | 耗时:{duration:F1}秒");
                    return true;
                }
                else
                {
                    var duration = (DateTime.Now - startTime).TotalSeconds;
                    LogManager.LogWarning($"上传失败 | 条码:{scannedBarcode} | 耗时:{duration:F1}秒 | 已保存到重试队列");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"上传异常 | 条码:{scannedBarcode} | 错误:{ex.Message}");

                // 确保数据已保存到队列中（双重保险）
                if (!string.IsNullOrEmpty(scannedBarcode))
                {
                    try
                    {
                        await AddToUploadQueue(scannedBarcode, completeData);
                    }
                    catch (Exception queueEx)
                    {
                        LogManager.LogError($"队列保存失败 | 条码:{scannedBarcode} | 错误:{queueEx.Message} | 启动本地文件备份");
                        await SaveToLocalFile(scannedBarcode, completeData);
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// 通过WebSocket上传数据到服务器
        /// </summary>
        private async Task<bool> TryUploadData(string barcode, string jsonData)
        {
            ClientWebSocket webSocket = null;
            try
            {

                webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                var serverUri = new Uri(_config.Server.WebSocketUrl);
                var connectToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

                LogManager.LogDebug($"连接WebSocket | 服务器:{serverUri}");
                await webSocket.ConnectAsync(serverUri, connectToken);

                // 🔥 优化：记录连接耗时
                LogManager.LogDebug($"连接成功 | 协议:{(serverUri.Scheme == "wss" ? "WSS" : "WS")}");

                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
                var sendToken = new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

                await webSocket.SendAsync(
                    new ArraySegment<byte>(dataBytes),
                    WebSocketMessageType.Text,
                    true,
                    sendToken);

                LogManager.LogDebug($"数据已发送 | 大小:{dataBytes.Length}B");

                var responseBuffer = new byte[1024];
                var receiveToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(responseBuffer),
                    receiveToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string serverResponse = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);

                    if (IsSuccessfulResponse(serverResponse, serverUri))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    LogManager.LogDebug($"数据发送成功 | 条码:{barcode} | 无文本响应");
                    return true;
                }
            }
            catch (WebSocketException wsEx)
            {
                LogManager.LogError($"WebSocket异常 | 条码:{barcode} | 错误:{wsEx.Message} | 错误代码:{wsEx.WebSocketErrorCode}");
                return false;
            }
            catch (TimeoutException timeEx)
            {
                LogManager.LogError($"WebSocket超时 | 条码:{barcode} | 错误:{timeEx.Message}");
                return false;
            }
            catch (OperationCanceledException cancelEx)
            {
                LogManager.LogError($"WebSocket取消 | 条码:{barcode} | 错误:{cancelEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"WebSocket上传异常 | 条码:{barcode} | 类型:{ex.GetType().Name} | 错误:{ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (webSocket?.State == WebSocketState.Open)
                    {
                        await CloseWebSocketSafely(webSocket);
                    }
                }
                catch (Exception cleanEx)
                {
                    LogManager.LogWarning($"关闭WebSocket异常 | 错误:{cleanEx.Message}");
                }
                finally
                {
                    webSocket?.Dispose();
                }
            }
        }


        /// <summary>
        /// 判断服务器响应是否成功
        /// </summary>
        private bool IsSuccessfulResponse(string response, Uri serverUri)
        {
            if (string.IsNullOrEmpty(response))
            {
                LogManager.LogWarning("上传响应为空");
                return false;
            }

            var successKeywords = _config.Server.SuccessKeywords ?? new[] { "success", "ok", "received", "完成", "成功" };

            foreach (var keyword in successKeywords)
            {
                if (response.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string successResponse = response.Length > 100 ? response.Substring(0, 100) + "..." : response;
                    LogManager.LogInfo($"上传确认 | 关键词:'{keyword}' | 响应:{successResponse}");
                    return true;
                }
            }

            string failedResponse = response.Length > 200 ? response.Substring(0, 200) + "..." : response;
            LogManager.LogWarning($"上传验证失败 | 服务器:{serverUri.Host}");
            LogManager.LogWarning($"  期望: [{string.Join(", ", successKeywords)}]");
            LogManager.LogWarning($"  实际: {failedResponse}");

            return false;
        }



        /// <summary>
        /// 安全关闭WebSocket连接
        /// </summary>
        private async Task CloseWebSocketSafely(ClientWebSocket webSocket)
        {
            try
            {
                // 先关闭输出流
                await webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    "Upload completed", 
                    CancellationToken.None);
                
                LogManager.LogInfo("WebSocket输出流已关闭");

                // 尝试接收关闭确认（超时处理）
                var buffer = new byte[1024];
                var timeout = new CancellationTokenSource(2000); // 2秒超时
                
                try
                {
                    while (webSocket.State == WebSocketState.CloseSent)
                    {
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), 
                            timeout.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            LogManager.LogInfo("收到WebSocket关闭确认");
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogManager.LogWarning("等待WebSocket关闭确认超时");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"安全关闭WebSocket过程中异常: {ex.Message}");
                
                // 如果正常关闭失败，强制中止连接
                try
                {
                    webSocket.Abort();
                }
                catch (Exception abortEx)
                {
                    LogManager.LogError($"强制中止WebSocket连接失败: {abortEx.Message}");
                }
            }
        }

        /// <summary>
        /// 添加到上传队列
        /// </summary>
        private async Task AddToUploadQueue(string barcode, string jsonData)
        {
            await ExecuteWithContext(async context =>
            {
                LogManager.LogInfo($"开始保存到上传队列: {barcode}");

                var existingRecord = await context.UploadQueue
                    .FirstOrDefaultAsync(q => q.Barcode == barcode);

                if (existingRecord != null)
                {
                    LogManager.LogInfo($"更新现有队列记录: {barcode}");
                    existingRecord.JsonData = jsonData;
                    existingRecord.LastRetryTime = DateTime.Now;
                    context.Entry(existingRecord).State = EntityState.Modified;
                }
                else
                {
                    LogManager.LogInfo($"创建新的队列记录: {barcode}");
                    var queueItem = new UploadQueue
                    {
                        Barcode = barcode,
                        JsonData = jsonData,
                        RetryCount = 0,
                        CreatedTime = DateTime.Now,
                        LastRetryTime = DateTime.Now
                    };
                    context.UploadQueue.Add(queueItem);
                }

                int savedCount = await context.SaveChangesAsync();
                LogManager.LogInfo($"队列保存成功，影响行数: {savedCount}，条码: {barcode}");
                return true;
            }, false);
        }

        /// <summary>
        /// 从队列中移除已成功上传的数据
        /// </summary>
        private async Task RemoveFromUploadQueue(string barcode)
        {
            await ExecuteWithContext(async context =>
            {
                var record = await context.UploadQueue
                    .FirstOrDefaultAsync(q => q.Barcode == barcode);

                if (record != null)
                {
                    context.UploadQueue.Remove(record);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"已从上传队列移除: {barcode}");
                }
                return true;
            }, false);
        }

        /// <summary>
        /// 更新产品数据的上传状态
        /// </summary>
        private async Task UpdateUploadStatus(string barcode, bool isUploaded)
        {
            await ExecuteWithContext(async context =>
            {
                var product = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product != null)
                {
                    product.IsUploaded = isUploaded;
                    product.UploadedTime = DateTime.Now;
                    context.Entry(product).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"更新上传状态成功: {barcode} -> {isUploaded}");
                }
                return true;
            }, false);
        }

        /// <summary>
        /// 紧急本地文件备份
        /// </summary>
        private async Task SaveToLocalFile(string barcode, string jsonData)
        {
            await Task.Run(() =>
            {
                try
                {
                    string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

                    if (!Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    string fileName = $"upload_backup_{DateTime.Now:yyyyMMdd_HHmmss}_{SanitizeFileName(barcode)}.json";
                    string filePath = Path.Combine(backupDir, fileName);

                    var backupData = new
                    {
                        Barcode = barcode,
                        JsonData = jsonData,
                        CreatedTime = DateTime.Now,
                        Reason = "数据库保存失败的紧急备份",
                        Version = "1.0"
                    };

                    string backupContent = JsonConvert.SerializeObject(backupData, Formatting.Indented);
                    File.WriteAllText(filePath, backupContent, Encoding.UTF8);

                    LogManager.LogInfo($"紧急备份到本地文件成功: {filePath}");
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"紧急备份失败: {ex.Message}");

                    try
                    {
                        string tempPath = Path.GetTempPath();
                        string tempFile = Path.Combine(tempPath, $"TailSystem_Emergency_{DateTime.Now:yyyyMMddHHmmss}.json");

                        var emergencyData = $"EMERGENCY_BACKUP_{DateTime.Now:yyyy-MM-dd HH:mm:ss}\nBarcode: {barcode}\nData: {jsonData}";
                        File.WriteAllText(tempFile, emergencyData);

                        LogManager.LogInfo($"临时目录紧急备份: {tempFile}");
                    }
                    catch (Exception tempEx)
                    {
                        LogManager.LogError($"临时目录备份也失败: {tempEx.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "UNKNOWN";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            if (fileName.Length > 50)
            {
                fileName = fileName.Substring(0, 50);
            }

            return fileName;
        }

        /// <summary>
        /// 检查备份目录大小，必要时清理旧文件
        /// </summary>
        private void CleanupBackupDirectory()
        {
            try
            {
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");

                if (!Directory.Exists(backupDir))
                    return;

                var files = Directory.GetFiles(backupDir, "*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                var filesToDelete = files.Skip(100);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        LogManager.LogInfo($"清理旧备份文件: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"删除备份文件失败: {ex.Message}");
                    }
                }

                long totalSize = files.Sum(f => f.Length);
                const long maxSize = 100 * 1024 * 1024; // 100MB

                if (totalSize > maxSize)
                {
                    LogManager.LogInfo($"备份目录大小超限({totalSize / 1024 / 1024}MB)，开始清理...");

                    var filesToDeleteBySize = files.OrderBy(f => f.CreationTime)
                        .TakeWhile(f => totalSize > maxSize / 2)
                        .ToList();

                    foreach (var file in filesToDeleteBySize)
                    {
                        try
                        {
                            totalSize -= file.Length;
                            file.Delete();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"清理备份目录异常: {ex.Message}");
            }
        }

        private async void ProcessRetryQueue(object state)
        {
            try
            {
                LogManager.LogInfo("开始检查重试队列...");

                var retryInterval = TimeSpan.FromMinutes(10);
                var cutoffTime = DateTime.Now.Subtract(retryInterval);

                var pendingItems = await ExecuteWithContext(async context =>
                {
                    return await context.UploadQueue
                        .Where(q => (q.RetryCount ?? 0) < MAX_RETRY_COUNT &&
                                   (q.LastRetryTime == null || q.LastRetryTime < cutoffTime))
                        .OrderBy(q => q.CreatedTime)
                        .Take(10)
                        .ToListAsync();
                }, new List<UploadQueue>());

                if (!pendingItems.Any())
                {
                    LogManager.LogInfo("重试队列为空或暂无需要重试的记录");
                    return;
                }

                LogManager.LogInfo($"发现 {pendingItems.Count} 条待重试记录");

                foreach (var item in pendingItems)
                {
                    var currentRetryCount = item.RetryCount ?? 0;
                    LogManager.LogInfo($"重试上传: {item.Barcode} (第{currentRetryCount + 1}次)");

                    bool success = await RetryUpload(item);

                    if (success)
                    {
                        await RemoveFromUploadQueue(item.Barcode);
                        LogManager.LogInfo($"重试上传成功，已从队列移除: {item.Barcode}");
                        await UpdateProductUploadStatus(item.Barcode, true);
                    }
                    else
                    {
                        await UpdateRetryCount(item.Barcode, currentRetryCount + 1);

                        if (currentRetryCount + 1 >= MAX_RETRY_COUNT)
                        {
                            LogManager.LogError($"重试次数已达上限({MAX_RETRY_COUNT})，停止重试: {item.Barcode}");
                        }
                    }

                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理重试队列异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新重试次数
        /// </summary>
        private async Task UpdateRetryCount(string barcode, int retryCount)
        {
            await ExecuteWithContext(async context =>
            {
                var record = await context.UploadQueue
                    .FirstOrDefaultAsync(q => q.Barcode == barcode);

                if (record != null)
                {
                    record.RetryCount = retryCount;
                    record.LastRetryTime = DateTime.Now;
                    context.Entry(record).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }
                return true;
            }, false);
        }

        /// <summary>
        /// 重试队列WebSocket上传
        /// </summary>
        private async Task<bool> RetryUpload(UploadQueue item)
        {
            try
            {
                LogManager.LogInfo($"重试WebSocket上传: {item.Barcode}");

                if (!await CheckNetworkConnection())
                {
                    LogManager.LogWarning("网络连接检查失败，跳过重试");
                    return false;
                }

                return await TryUploadData(item.Barcode, item.JsonData);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"重试WebSocket上传异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查到服务器的网络连接
        /// </summary>
        private async Task<bool> CheckNetworkConnection()
        {
            try
            {
                LogManager.LogInfo($"检查网络连接...");

                using (var testSocket = new ClientWebSocket())
                {
                    var testUri = new Uri(_config.Server.WebSocketUrl);
                    var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
                    await testSocket.ConnectAsync(testUri, timeout);
                    if (testSocket.State == WebSocketState.Open)
                    {
                        await testSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            "Connection test", CancellationToken.None);
                        LogManager.LogInfo($"网络连接正常");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"网络连接检查失败: {ex.Message}");
                try
                {
                    var uri = new Uri(_config.Server.WebSocketUrl);
                    var hostName = uri.Host;
                    using (var ping = new System.Net.NetworkInformation.Ping())
                    {
                        var reply = await ping.SendPingAsync(hostName, 3000);
                        bool pingSuccess = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                        LogManager.LogInfo($"Ping测试结果: {(pingSuccess ? "成功" : "失败")}");
                        return pingSuccess;
                    }
                }
                catch (Exception pingEx)
                {
                    LogManager.LogError($"Ping测试失败: {pingEx.Message}");
                    return false;
                }
            }
        }

        private async Task UpdateProductUploadStatus(string barcode, bool isUploaded)
        {
            await ExecuteWithContext(async context =>
            {
                var product = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product != null)
                {
                    product.IsUploaded = isUploaded;
                    if (isUploaded)
                    {
                        product.UploadedTime = DateTime.Now;
                    }
                    context.Entry(product).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }
                return true;
            }, false);
        }

        public async Task<(int total, int exceeded, int pending)> GetRetryQueueStats()
        {
            return await ExecuteWithContext(async context =>
            {
                var total = await context.UploadQueue.CountAsync();
                var exceeded = await context.UploadQueue.CountAsync(q => (q.RetryCount ?? 0) >= MAX_RETRY_COUNT);
                var pending = total - exceeded;
                return (total, exceeded, pending);
            }, (0, 0, 0));
        }

        public async Task<bool> MarkProductAsCompleted(string barcode)
        {
            return await ExecuteWithContext(async context =>
            {
                var product = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product != null)
                {
                    product.IsCompleted = true;
                    product.CompletedTime = DateTime.Now;
                    context.Entry(product).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }, false);
        }

        public async Task<List<ProductData>> GetUnuploadedProducts()
        {
            return await _dataService.GetUnuploadedProducts();
        }
        public async Task<ProductData> GetProductByBarcode(string barcode)
        {
            return await _dataService.GetProductByBarcode(barcode);
        }
        public async Task<List<ProductData>> GetProductDataHistory(int days = 30)
        {
            return await _dataService.GetProductDataHistory(days);
        }
        public async Task<List<ProductData>> GetAllProductData()
        {
            return await _dataService.GetAllProductData();
        }
        public async Task<List<ProductData>> SearchProductData(
            string barcode = null,
            bool? isCompleted = null,
            bool? isUploaded = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return await _dataService.SearchProductData(barcode, isCompleted, isUploaded, startDate, endDate);
        }
        public async Task<DataService.ProductDataStats> GetProductDataStats()
        {
            return await _dataService.GetProductDataStats();
        }
        public async Task<bool> DeleteProductData(long id)
        {
            return await _dataService.DeleteProductData(id);
        }
        public async Task<int> DeleteProductDataBatch(List<long> ids)
        {
            return await _dataService.DeleteProductDataBatch(ids);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            retryTimer?.Dispose();
            cleanupTimer?.Dispose();
            httpClient?.Dispose();
            _dataService?.Dispose();
            LogManager.LogInfo("DataManager已释放");
        }

       
    }
}
