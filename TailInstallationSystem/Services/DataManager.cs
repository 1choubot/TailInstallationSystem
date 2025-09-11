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

namespace TailInstallationSystem
{
    public class DataManager : IDisposable
    {
        private readonly NodeInstrumentMESEntities context;
        private readonly HttpClient httpClient;
        private System.Threading.Timer retryTimer;
        private System.Threading.Timer cleanupTimer;
        private const int MAX_RETRY_COUNT = 5;

        public DataManager()
        {
            context = new NodeInstrumentMESEntities();
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            retryTimer = new System.Threading.Timer(ProcessRetryQueue, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

            // 启动备份清理定时器（每天清理一次）
            cleanupTimer = new System.Threading.Timer(state => CleanupBackupDirectory(), null,
                TimeSpan.FromHours(1), TimeSpan.FromHours(24));
        }

        /// <summary>
        /// 保存产品数据 - 统一使用当前工序条码
        /// </summary>
        public async Task<bool> SaveProductData(string currentBarcode, string[] processDataArray,
            string tailProcessData, string completeData)
        {
            try
            {
                LogManager.LogInfo($"开始保存当前工序数据: {currentBarcode}");

                // 检查是否已存在相同条码的记录
                var existingProduct = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == currentBarcode);

                if (existingProduct != null)
                {
                    LogManager.LogInfo($"更新现有产品记录: {currentBarcode}");

                    // 更新现有记录
                    existingProduct.Process1_Data = processDataArray.Length > 0 ? processDataArray[0] : existingProduct.Process1_Data;
                    existingProduct.Process2_Data = processDataArray.Length > 1 ? processDataArray[1] : existingProduct.Process2_Data;
                    existingProduct.Process3_Data = processDataArray.Length > 2 ? processDataArray[2] : existingProduct.Process3_Data;
                    existingProduct.Process4_Data = tailProcessData;
                    existingProduct.CompleteData = completeData;
                    existingProduct.CompletedTime = DateTime.Now;
                    existingProduct.IsCompleted = true;
                    existingProduct.IsUploaded = false; // 重置上传状态，等待重新上传

                    context.Entry(existingProduct).State = EntityState.Modified;
                }
                else
                {
                    LogManager.LogInfo($"创建新的产品记录: {currentBarcode}");

                    // 创建新记录
                    var product = new ProductData
                    {
                        Barcode = currentBarcode,  // 使用当前工序条码作为唯一标识
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
            }
            catch (Exception ex)
            {
                LogManager.LogError($"当前工序数据保存失败: {currentBarcode}");
                LogManager.LogError($"异常消息: {ex.Message}");
                LogManager.LogError($"内部异常: {ex.InnerException?.Message}");
                return false;
            }
        }

        /// <summary>
        /// 上传数据到服务器 - 使用扫描条码作为标识
        /// </summary>
        public async Task<bool> UploadToServer(string scannedBarcode, string completeData)
        {
            try
            {
                LogManager.LogInfo($"开始上传当前工序数据: {scannedBarcode}");

                // 先保存到重试队列（确保数据不丢失）
                await AddToUploadQueue(scannedBarcode, completeData);
                LogManager.LogInfo($"数据已保存到上传队列: {scannedBarcode}");

                // 立即尝试上传
                bool uploadSuccess = await TryUploadData(scannedBarcode, completeData);

                if (uploadSuccess)
                {
                    // 上传成功，从队列中移除
                    await RemoveFromUploadQueue(scannedBarcode);
                    await UpdateUploadStatus(scannedBarcode, true);
                    LogManager.LogInfo($"数据上传成功并已从队列移除: {scannedBarcode}");
                    return true;
                }
                else
                {
                    LogManager.LogInfo($"上传失败，数据已保存在重试队列中: {scannedBarcode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"上传流程异常: {ex.Message}");

                // 确保数据已保存到队列中（双重保险）
                if (!string.IsNullOrEmpty(scannedBarcode))
                {
                    try
                    {
                        await AddToUploadQueue(scannedBarcode, completeData);
                    }
                    catch (Exception queueEx)
                    {
                        LogManager.LogError($"紧急保存到队列也失败: {queueEx.Message}");
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
                LogManager.LogInfo($"尝试通过WebSocket上传数据: {barcode}");

                webSocket = new ClientWebSocket();

                // 配置WebSocket选项
                webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                // 连接到WebSocket服务器
                var serverUri = new Uri("ws://124.222.6.60:8800");
                var connectToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

                LogManager.LogInfo($"正在连接WebSocket服务器: {serverUri}");
                await webSocket.ConnectAsync(serverUri, connectToken);

                LogManager.LogInfo($"WebSocket连接已建立");

                // 发送JSON数据
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
                var sendToken = new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;

                await webSocket.SendAsync(
                    new ArraySegment<byte>(dataBytes),
                    WebSocketMessageType.Text,
                    true,
                    sendToken);

                LogManager.LogInfo($"数据已发送至服务器，数据大小: {dataBytes.Length} 字节");

                // 等待服务器响应
                var responseBuffer = new byte[1024];
                var receiveToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(responseBuffer),
                    receiveToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string serverResponse = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);
                    LogManager.LogInfo($"服务器响应: {serverResponse}");

                    // 根据服务器响应判断成功与否
                    if (serverResponse.Contains("success") || serverResponse.Contains("ok") ||
                        serverResponse.Contains("received"))
                    {
                        LogManager.LogInfo($"服务器确认数据接收成功: {barcode}");
                        return true;
                    }
                    else
                    {
                        LogManager.LogWarning($"服务器响应异常: {serverResponse}");
                        return false;
                    }
                }
                else
                {
                    LogManager.LogInfo($"数据发送成功（无文本响应）: {barcode}");
                    return true; // 如果服务器不返回文本响应，可以认为发送成功
                }
            }
            catch (WebSocketException wsEx)
            {
                LogManager.LogError($"WebSocket异常: {wsEx.Message}，条码: {barcode}");
                LogManager.LogError($"WebSocket错误代码: {wsEx.WebSocketErrorCode}");
                return false;
            }
            catch (TimeoutException timeEx)
            {
                LogManager.LogError($"WebSocket连接超时: {timeEx.Message}，条码: {barcode}");
                return false;
            }
            catch (OperationCanceledException cancelEx)
            {
                LogManager.LogError($"WebSocket操作被取消: {cancelEx.Message}，条码: {barcode}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"WebSocket上传异常: {ex.Message}，条码: {barcode}");
                LogManager.LogError($"异常类型: {ex.GetType().Name}");
                return false;
            }
            finally
            {
                // 清理资源
                try
                {
                    if (webSocket?.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Upload completed",
                            CancellationToken.None);
                        LogManager.LogInfo($"WebSocket连接已正常关闭");
                    }
                }
                catch (Exception cleanEx)
                {
                    LogManager.LogWarning($"关闭WebSocket时异常: {cleanEx.Message}");
                }
                finally
                {
                    webSocket?.Dispose();
                }
            }
        }

        /// <summary>
        /// 添加到上传队列
        /// </summary>
        private async Task AddToUploadQueue(string barcode, string jsonData)
        {
            try
            {
                LogManager.LogInfo($"开始保存到上传队列: {barcode}");

                // 检查是否已存在记录
                var existingRecord = await context.UploadQueue
                    .FirstOrDefaultAsync(q => q.Barcode == barcode);

                if (existingRecord != null)
                {
                    LogManager.LogInfo($"更新现有队列记录: {barcode}");

                    // 更新现有记录
                    existingRecord.JsonData = jsonData;
                    existingRecord.LastRetryTime = DateTime.Now;
                    // 不重置 RetryCount，保持累计重试次数

                    context.Entry(existingRecord).State = EntityState.Modified;
                }
                else
                {
                    LogManager.LogInfo($"创建新的队列记录: {barcode}");

                    // 创建新记录
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

                // 强制保存
                int savedCount = await context.SaveChangesAsync();
                LogManager.LogInfo($"队列保存成功，影响行数: {savedCount}，条码: {barcode}");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                LogManager.LogError($"数据库验证错误: {barcode}");
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    LogManager.LogError($"实体: {validationErrors.Entry.Entity.GetType().Name}");
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        LogManager.LogError($"属性: {validationError.PropertyName}");
                        LogManager.LogError($"错误: {validationError.ErrorMessage}");
                    }
                }
                throw;
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpdateEx)
            {
                LogManager.LogError($"数据库更新错误: {barcode}");
                LogManager.LogError($"异常: {dbUpdateEx.Message}");

                Exception currentEx = dbUpdateEx.InnerException;
                int level = 1;
                while (currentEx != null)
                {
                    LogManager.LogError($"内部异常[{level}]: {currentEx.GetType().Name}");
                    LogManager.LogError($"消息: {currentEx.Message}");
                    currentEx = currentEx.InnerException;
                    level++;
                }
                throw;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存到上传队列失败: {barcode}");
                LogManager.LogError($"异常类型: {ex.GetType().Name}");
                LogManager.LogError($"异常消息: {ex.Message}");

                Exception currentEx = ex.InnerException;
                int level = 1;
                while (currentEx != null)
                {
                    LogManager.LogError($"内部异常[{level}]: {currentEx.GetType().Name}");
                    LogManager.LogError($"消息: {currentEx.Message}");
                    currentEx = currentEx.InnerException;
                    level++;
                }

                LogManager.LogError($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 从队列中移除已成功上传的数据
        /// </summary>
        private async Task RemoveFromUploadQueue(string barcode)
        {
            try
            {
                var record = await context.UploadQueue
                    .FirstOrDefaultAsync(q => q.Barcode == barcode);

                if (record != null)
                {
                    context.UploadQueue.Remove(record);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"已从上传队列移除: {barcode}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"从队列移除记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新产品数据的上传状态
        /// </summary>
        private async Task UpdateUploadStatus(string barcode, bool isUploaded)
        {
            try
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
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新上传状态失败: {ex.Message}");
            }
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

                    // 确保备份目录存在
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

                    // 使用同步方法确保兼容性
                    File.WriteAllText(filePath, backupContent, Encoding.UTF8);

                    LogManager.LogInfo($"紧急备份到本地文件成功: {filePath}");
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"紧急备份失败: {ex.Message}");

                    // 最后的尝试：写入到临时目录
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

            // 移除或替换文件名中的非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            // 限制文件名长度
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

                // 保留最新的100个文件，删除其余的
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

                // 检查目录大小（如果超过100MB，删除最老的文件）
                long totalSize = files.Sum(f => f.Length);
                const long maxSize = 100 * 1024 * 1024; // 100MB

                if (totalSize > maxSize)
                {
                    LogManager.LogInfo($"备份目录大小超限({totalSize / 1024 / 1024}MB)，开始清理...");

                    var filesToDeleteBySize = files.OrderBy(f => f.CreationTime)
                        .TakeWhile(f => totalSize > maxSize / 2) // 清理到50MB以下
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

                var pendingItems = await context.UploadQueue
                    .Where(q => (q.RetryCount ?? 0) < MAX_RETRY_COUNT &&
                               (q.LastRetryTime == null || q.LastRetryTime < cutoffTime))
                    .OrderBy(q => q.CreatedTime)
                    .Take(10)
                    .ToListAsync();

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
                        context.UploadQueue.Remove(item);
                        LogManager.LogInfo($"重试上传成功，已从队列移除: {item.Barcode}");
                        await UpdateProductUploadStatus(item.Barcode, true);
                    }
                    else
                    {
                        item.RetryCount = currentRetryCount + 1;
                        item.LastRetryTime = DateTime.Now;

                        if (item.RetryCount >= MAX_RETRY_COUNT)
                        {
                            LogManager.LogError($"重试次数已达上限({MAX_RETRY_COUNT})，停止重试: {item.Barcode}");
                        }
                    }

                    await context.SaveChangesAsync();
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理重试队列异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 重试队列WebSocket上传
        /// </summary>
        private async Task<bool> RetryUpload(UploadQueue item)
        {
            try
            {
                LogManager.LogInfo($"重试WebSocket上传: {item.Barcode}");

                // 检查网络连接
                if (!await CheckNetworkConnection())
                {
                    LogManager.LogWarning("网络连接检查失败，跳过重试");
                    return false;
                }

                // 使用相同的WebSocket上传逻辑
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

                // 直接测试WebSocket服务器连接
                using (var testSocket = new ClientWebSocket())
                {
                    var testUri = new Uri("ws://124.222.6.60:8800");
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

                // 备选方案：ping测试
                try
                {
                    using (var ping = new System.Net.NetworkInformation.Ping())
                    {
                        var reply = await ping.SendPingAsync("124.222.6.60", 3000);
                        bool pingSuccess = reply.Status == System.Net.NetworkInformation.IPStatus.Success;

                        LogManager.LogInfo($"Ping测试结果: {(pingSuccess ? "成功" : "失败")}");
                        return pingSuccess;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task UpdateProductUploadStatus(string barcode, bool isUploaded)
        {
            try
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
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新产品状态失败: {ex.Message}");
            }
        }

        public async Task<(int total, int exceeded, int pending)> GetRetryQueueStats()
        {
            try
            {
                var total = await context.UploadQueue.CountAsync();
                var exceeded = await context.UploadQueue.CountAsync(q => (q.RetryCount ?? 0) >= MAX_RETRY_COUNT);
                var pending = total - exceeded;
                return (total, exceeded, pending);
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        public async Task<List<ProductData>> GetUnuploadedProducts()
        {
            try
            {
                return await context.ProductData
                    .Where(p => p.IsUploaded == false || p.IsUploaded == null)
                    .OrderBy(p => p.CreatedTime)
                    .ToListAsync();
            }
            catch
            {
                return new List<ProductData>();
            }
        }

        public async Task<ProductData> GetProductByBarcode(string barcode)
        {
            try
            {
                return await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> MarkProductAsCompleted(string barcode)
        {
            try
            {
                var product = await context.ProductData
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product != null)
                {
                    product.IsCompleted = true;
                    product.CompletedTime = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"标记产品完成状态失败: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ProductData>> GetProductDataHistory(int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-days);

                var productDataList = await context.ProductData
                    .Where(p => p.CreatedTime >= cutoffDate)
                    .OrderByDescending(p => p.CreatedTime)
                    .ToListAsync();
                LogManager.LogInfo($"获取了最近 {days} 天的 {productDataList.Count} 条产品数据");
                return productDataList;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"获取产品数据历史记录失败: {ex.Message}");
                return new List<ProductData>();
            }
        }

        public async Task<List<ProductData>> GetAllProductData()
        {
            try
            {
                var productDataList = await context.ProductData
                    .OrderByDescending(p => p.CreatedTime)
                    .ToListAsync();
                LogManager.LogInfo($"获取了所有 {productDataList.Count} 条产品数据");
                return productDataList;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"获取所有产品数据失败: {ex.Message}");
                return new List<ProductData>();
            }
        }

        public async Task<List<ProductData>> SearchProductData(
            string barcode = null,
            bool? isCompleted = null,
            bool? isUploaded = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var query = context.ProductData.AsQueryable();

                if (!string.IsNullOrEmpty(barcode))
                {
                    query = query.Where(p => p.Barcode.Contains(barcode));
                }

                if (isCompleted.HasValue)
                {
                    query = query.Where(p => p.IsCompleted == isCompleted.Value);
                }

                if (isUploaded.HasValue)
                {
                    query = query.Where(p => p.IsUploaded == isUploaded.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.CreatedTime >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(p => p.CreatedTime <= endDate.Value);
                }

                var result = await query.OrderByDescending(p => p.CreatedTime).ToListAsync();
                LogManager.LogInfo($"搜索产品数据，找到 {result.Count} 条记录");
                return result;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"搜索产品数据失败: {ex.Message}");
                return new List<ProductData>();
            }
        }

        public async Task<ProductDataStats> GetProductDataStats()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var stats = new ProductDataStats
                {
                    TotalCount = await context.ProductData.CountAsync(),
                    CompletedCount = await context.ProductData.CountAsync(p => p.IsCompleted == true),
                    UploadedCount = await context.ProductData.CountAsync(p => p.IsUploaded == true),
                    TodayCount = await context.ProductData.CountAsync(p => p.CreatedTime >= today),
                    WeekCount = await context.ProductData.CountAsync(p => p.CreatedTime >= thisWeek),
                    MonthCount = await context.ProductData.CountAsync(p => p.CreatedTime >= thisMonth),
                    PendingUploadCount = await context.ProductData.CountAsync(p => p.IsUploaded != true)
                };
                return stats;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"获取产品数据统计失败: {ex.Message}");
                return new ProductDataStats();
            }
        }

        public async Task<bool> DeleteProductData(long id)
        {
            try
            {
                var product = await context.ProductData.FindAsync(id);
                if (product != null)
                {
                    context.ProductData.Remove(product);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"删除产品数据成功: ID={id}, Barcode={product.Barcode}");
                    return true;
                }

                LogManager.LogWarning($"未找到要删除的产品数据: ID={id}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"删除产品数据失败: {ex.Message}");
                return false;
            }
        }

        public async Task<int> DeleteProductDataBatch(List<long> ids)
        {
            try
            {
                var products = await context.ProductData
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();
                if (products.Any())
                {
                    context.ProductData.RemoveRange(products);
                    await context.SaveChangesAsync();
                    LogManager.LogInfo($"批量删除产品数据成功: {products.Count} 条");
                    return products.Count;
                }
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"批量删除产品数据失败: {ex.Message}");
                return 0;
            }
        }

        public void Dispose()
        {
            retryTimer?.Dispose();
            cleanupTimer?.Dispose();
            httpClient?.Dispose();
            context?.Dispose();
        }

        /// <summary>
        /// 产品数据统计信息
        /// </summary>
        public class ProductDataStats
        {
            public int TotalCount { get; set; }
            public int CompletedCount { get; set; }
            public int UploadedCount { get; set; }
            public int TodayCount { get; set; }
            public int WeekCount { get; set; }
            public int MonthCount { get; set; }
            public int PendingUploadCount { get; set; }
            public double CompletionRate => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
            public double UploadRate => TotalCount > 0 ? (double)UploadedCount / TotalCount * 100 : 0;
        }
    }
}
