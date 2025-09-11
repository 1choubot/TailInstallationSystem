using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TailInstallationSystem.Models;

namespace TailInstallationSystem
{
    public class TailInstallationController
    {
        private CommunicationManager commManager;
        private DataManager dataManager;
        private List<string> receivedProcessData = new List<string>();
        private bool isRunning = false;
        private TaskCompletionSource<string> barcodeWaitTask;

        // 扫码缓存机制
        private string cachedBarcode = null;
        private readonly object barcodeLock = new object();

        public TailInstallationController(CommunicationManager communicationManager)
        {
            commManager = communicationManager;
            dataManager = new DataManager();

            // 绑定事件
            commManager.OnDataReceived += ProcessReceivedData;
            commManager.OnBarcodeScanned += ProcessBarcodeData;
            commManager.OnScrewDataReceived += ProcessScrewData;
        }

        public async Task StartSystem()
        {
            await commManager.InitializeConnections();
            isRunning = true;

            // 启动主工作循环
            _ = Task.Run(MainWorkLoop);
        }

        public async Task StopSystem()
        {
            isRunning = false;
            commManager.Dispose();
        }

        private async Task MainWorkLoop()
        {
            while (isRunning)
            {
                try
                {
                    // 检查PLC触发信号
                    if (await commManager.CheckPLCTrigger())
                    {
                        // 发送确认信号
                        await commManager.SendPLCConfirmation();

                        // 执行尾椎安装流程
                        await ExecuteTailInstallation();
                    }

                    await Task.Delay(100); // 100ms检查间隔
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"主工作循环异常: {ex.Message}");
                }
            }
        }

        private void ProcessReceivedData(string jsonData)
        {
            try
            {
                // 解析并存储前三道工序数据
                receivedProcessData.Add(jsonData);
                LogManager.LogInfo($"接收到工序数据: {jsonData.Substring(0, Math.Min(50, jsonData.Length))}...");

                // 检查是否收齐数据
                if (receivedProcessData.Count >= 3)
                {
                    LogManager.LogInfo("前三道工序数据已收齐，准备执行尾椎安装");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理接收数据异常: {ex.Message}");
            }
        }

        private void ProcessBarcodeData(string barcode)
        {
            lock (barcodeLock)
            {
                LogManager.LogInfo($"扫描到条码: {barcode}");

                // 缓存扫码数据
                cachedBarcode = barcode;
                LogManager.LogInfo($"条码已缓存: {barcode}");

                // 如果正在等待条码扫描，则完成等待任务
                if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                {
                    LogManager.LogInfo("条码扫描等待任务已完成");
                    barcodeWaitTask.SetResult(barcode);
                    barcodeWaitTask = null;
                    cachedBarcode = null; // 清空缓存（已使用）
                }
            }
        }

        private void ProcessScrewData(string screwData)
        {
            LogManager.LogInfo($"螺丝机数据: {screwData}");
            // 解析螺丝机返回的扭矩等数据
        }

        private async Task ExecuteTailInstallation()
        {
            try
            {
                // 确保有足够的工序数据
                if (receivedProcessData.Count < 3)
                {
                    LogManager.LogWarning("前三道工序数据不完整，无法执行尾椎安装");
                    return;
                }

                LogManager.LogInfo("开始执行尾椎安装工序");

                // 1. 等待条码扫描
                string barcode = await WaitForBarcodeScan();

                // 2. 执行螺丝安装
                var screwResult = await PerformScrewInstallation();

                // 3. 生成本工序数据
                var tailProcessData = GenerateTailProcessData(barcode, screwResult);

                // 4. 整合所有数据
                var completeData = CombineAllProcessData(tailProcessData);

                // 5. 保存到本地数据库
                await dataManager.SaveProductData(barcode, receivedProcessData.ToArray(), tailProcessData, completeData);

                // 6. 上传到服务器
                bool uploadSuccess = await dataManager.UploadToServer(barcode, completeData);

                if (uploadSuccess)
                {
                    LogManager.LogInfo($"产品 {barcode} 数据上传成功");
                }
                else
                {
                    LogManager.LogWarning($"产品 {barcode} 数据上传失败，已加入重试队列");
                }

                LogManager.LogInfo("尾椎安装工序完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"尾椎安装异常: {ex.Message}");
            }
            finally
            {
                // 清空已处理的数据，准备下一个产品
                LogManager.LogInfo("清空已处理的工序数据，准备处理下一个产品");
                receivedProcessData.Clear();
            }
        }

        private async Task<string> WaitForBarcodeScan()
        {
            LogManager.LogInfo("等待条码扫描...");

            lock (barcodeLock)
            {
                // 如果已经有缓存的条码，直接使用
                if (!string.IsNullOrEmpty(cachedBarcode))
                {
                    LogManager.LogInfo($"使用缓存的条码: {cachedBarcode}");
                    string result = cachedBarcode;
                    cachedBarcode = null; // 清空缓存
                    return result;
                }
            }

            LogManager.LogInfo("缓存中无条码，等待新的扫码数据...");

            // 创建等待任务
            barcodeWaitTask = new TaskCompletionSource<string>();

            // 设置超时（30秒）
            var timeoutTask = Task.Delay(30000);
            var completedTask = await Task.WhenAny(barcodeWaitTask.Task, timeoutTask);

            if (completedTask == barcodeWaitTask.Task)
            {
                LogManager.LogInfo("条码扫描完成");
                return barcodeWaitTask.Task.Result;
            }
            else
            {
                LogManager.LogError("等待条码扫描超时（30秒）");
                throw new TimeoutException("等待条码扫描超时");
            }
        }

        private async Task<ScrewInstallationResult> PerformScrewInstallation()
        {
            LogManager.LogInfo("开始执行螺丝安装...");

            // 发送螺丝机启动命令
            await commManager.SendScrewDriverCommand("START_SCREW_INSTALLATION");

            // 等待螺丝安装完成（实际应该监听螺丝机返回的完成信号）
            await Task.Delay(5000); // 预计安装时间

            LogManager.LogInfo("螺丝安装完成");

            // 实际项目中应该解析螺丝机返回的真实扭矩数据
            return new ScrewInstallationResult
            {
                Torque = 2.5m, // 这里应该从螺丝机实际返回数据中解析
                InstallTime = DateTime.Now,
                Success = true // 这里应该根据螺丝机返回的状态判断
            };
        }

        private string GenerateTailProcessData(string barcode, ScrewInstallationResult screwResult)
        {
            LogManager.LogInfo($"生成尾椎安装工序数据: {barcode}");

            var testItems = new[]
            {
                new 
                {
                    Id = 37,
                    ItemName = "尾椎安装扭矩",
                    ItemType = "Torque"
                }
            };

            var processData = new
            {
                processName = "安装尾椎",
                barcodes = new[]
                {
                    new { codeType = "A", barcode = barcode }
                },
                testItems = testItems.Select(item => new
                {
                    id = item.Id,
                    itemName = item.ItemName,
                    itemType = item.ItemType,
                    pass = screwResult.Success,
                    resultText = screwResult.Success ? "扭矩达标" : "扭矩不足",
                    // 添加扭矩详细数据作为子项
                    subItems = item.Id == 37 ? new[]
                    {
                        new
                        {
                            name = "Torque",
                            value = (double)screwResult.Torque,
                            unit = "Nm",
                            pass = screwResult.Success
                        }
                    } : null
                }).ToArray()
            };

            string jsonResult = JsonConvert.SerializeObject(processData, Formatting.Indented);
            LogManager.LogInfo($"尾椎安装工序数据生成完成");
            LogManager.LogInfo($"测试项数量: {testItems.Length}");
            LogManager.LogInfo($"扭矩值: {screwResult.Torque}Nm");
            LogManager.LogInfo($"测试结果: {(screwResult.Success ? "通过" : "失败")}");

            return jsonResult;
        }

        private string CombineAllProcessData(string tailProcessData)
        {
            var allProcesses = new List<object>();

            // 添加前三道工序数据
            foreach (var processJson in receivedProcessData)
            {
                allProcesses.Add(JsonConvert.DeserializeObject(processJson));
            }

            // 添加尾椎安装数据
            allProcesses.Add(JsonConvert.DeserializeObject(tailProcessData));

            string combinedData = JsonConvert.SerializeObject(allProcesses, Formatting.Indented);
            LogManager.LogInfo($"合并所有工序数据完成，总共 {allProcesses.Count} 道工序");

            return combinedData;
        }

        public void EmergencyStop()
        {
            try
            {
                isRunning = false;
                LogManager.LogWarning("执行紧急停止");

                // 清理等待任务
                if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                {
                    barcodeWaitTask.SetCanceled();
                    barcodeWaitTask = null;
                }

                // 清空缓存
                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                // 立即断开所有连接
                commManager?.Dispose();

                LogManager.LogWarning("系统已紧急停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"紧急停止异常: {ex.Message}");
            }
        }
    }

    public class ScrewInstallationResult
    {
        public decimal Torque { get; set; }
        public DateTime InstallTime { get; set; }
        public bool Success { get; set; }
    }
}
