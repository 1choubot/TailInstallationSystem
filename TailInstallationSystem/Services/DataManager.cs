using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TailInstallationSystem
{
    public class DataManager
    {
        public async Task SaveProductData(string barcode, string[] processDataArray, string tailProcessData, string completeData)
        {
            try
            {
                using (var db = new NodeInstrumentMESEntities())
                {
                    var productData = new ProductData
                    {
                        Barcode = barcode,
                        Process1_Data = processDataArray.Length > 0 ? processDataArray[0] : null,
                        Process2_Data = processDataArray.Length > 1 ? processDataArray[1] : null,
                        Process3_Data = processDataArray.Length > 2 ? processDataArray[2] : null,
                        Process4_Data = tailProcessData,
                        CompleteData = completeData,
                        IsCompleted = true,
                        IsUploaded = false,
                        CreatedTime = DateTime.Now,
                        CompletedTime = DateTime.Now
                    };

                    db.ProductData.Add(productData);
                    await db.SaveChangesAsync();
                    LogManager.LogInfo($"产品数据已保存到本地数据库: {barcode}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存产品数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UploadToServer(string completeData)
        {
            try
            {
                using (var webSocket = new ClientWebSocket())
                {
                    // WebSocket服务器地址 - 根据您的实际配置修改
                    var uri = new Uri("ws://192.168.1.100:9001");
                    await webSocket.ConnectAsync(uri, CancellationToken.None);

                    byte[] data = Encoding.UTF8.GetBytes(completeData);
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );

                    // 等待服务器响应
                    var buffer = new byte[1024];
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        LogManager.LogInfo($"服务器响应: {response}");

                        // 更新数据库上传状态
                        await UpdateUploadStatus(ExtractBarcodeFromJson(completeData), true);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"上传数据失败: {ex.Message}");

                // 上传失败，添加到重试队列
                await AddToUploadQueue(completeData);
                return false;
            }

            return false;
        }

        private async Task AddToUploadQueue(string jsonData)
        {
            try
            {
                using (var db = new NodeInstrumentMESEntities())
                {
                    var uploadItem = new UploadQueue
                    {
                        Barcode = ExtractBarcodeFromJson(jsonData),
                        JsonData = jsonData,
                        RetryCount = 0,
                        CreatedTime = DateTime.Now,
                        LastRetryTime = DateTime.Now
                    };

                    db.UploadQueue.Add(uploadItem);
                    await db.SaveChangesAsync();
                    LogManager.LogInfo("数据已添加到上传重试队列");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"添加到上传队列失败: {ex.Message}");
            }
        }

        public async Task RetryFailedUploads()
        {
            try
            {
                using (var db = new NodeInstrumentMESEntities())
                {
                    var failedUploads = db.UploadQueue
                        .Where(u => u.RetryCount < 5) // 最多重试5次
                        .Take(10) // 每次处理10条
                        .ToList();

                    foreach (var upload in failedUploads)
                    {
                        bool success = await UploadToServer(upload.JsonData);

                        if (success)
                        {
                            // 上传成功，从队列中移除
                            db.UploadQueue.Remove(upload);
                            LogManager.LogInfo($"重试上传成功: {upload.Barcode}");
                        }
                        else
                        {
                            // 增加重试次数
                            upload.RetryCount++;
                            upload.LastRetryTime = DateTime.Now;

                            if (upload.RetryCount >= 5)
                            {
                                LogManager.LogError($"数据重试次数已达上限: {upload.Barcode}");
                            }
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"重试上传失败: {ex.Message}");
            }
        }

        private async Task UpdateUploadStatus(string barcode, bool isUploaded)
        {
            try
            {
                using (var db = new NodeInstrumentMESEntities())
                {
                    var productData = db.ProductData.FirstOrDefault(p => p.Barcode == barcode);
                    if (productData != null)
                    {
                        productData.IsUploaded = isUploaded;
                        if (isUploaded)
                        {
                            productData.UploadedTime = DateTime.Now;
                        }
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新上传状态失败: {ex.Message}");
            }
        }

        private string ExtractBarcodeFromJson(string jsonData)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(jsonData);
                if (data is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                {
                    var firstProcess = array[0];
                    var barcodes = firstProcess["barcodes"];
                    if (barcodes != null && barcodes.Count() > 0)
                    {
                        return barcodes[0]["barcode"]?.ToString() ?? "UNKNOWN";
                    }
                }
                return "UNKNOWN";
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        public async Task<List<ProductData>> GetProductDataHistory(int days = 7)
        {
            try
            {
                using (var db = new NodeInstrumentMESEntities())
                {
                    var startDate = DateTime.Now.AddDays(-days);
                    return db.ProductData
                        .Where(p => p.CreatedTime >= startDate)
                        .OrderByDescending(p => p.CreatedTime)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"获取产品数据历史失败: {ex.Message}");
                return new List<ProductData>();
            }
        }
    }
}