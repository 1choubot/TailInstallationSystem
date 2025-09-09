using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TailInstallationSystem
{
    public class TailInstallationController
    {
        private CommunicationManager commManager;
        private DataManager dataManager;
        private List<string> receivedProcessData = new List<string>();
        private bool isRunning = false;

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
                    // 记录异常日志
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
            LogManager.LogInfo($"扫描到条码: {barcode}");
            // 验证条码格式等
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
                bool uploadSuccess = await dataManager.UploadToServer(completeData);

                if (uploadSuccess)
                {
                    LogManager.LogInfo($"产品 {barcode} 数据上传成功");
                }
                else
                {
                    LogManager.LogWarning($"产品 {barcode} 数据上传失败，已加入重试队列");
                }

                // 清空已处理的数据
                receivedProcessData.Clear();

                LogManager.LogInfo("尾椎安装工序完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"尾椎安装异常: {ex.Message}");
            }
        }

        private async Task<string> WaitForBarcodeScan()
        {
            // 实现等待条码扫描的逻辑
            // 这里简化处理，实际应该有超时机制
            return await Task.FromResult("C123456");
        }

        private async Task<ScrewInstallationResult> PerformScrewInstallation()
        {
            // 实现螺丝安装逻辑
            await Task.Delay(2000); // 模拟安装时间

            return new ScrewInstallationResult
            {
                Torque = 2.5m,
                InstallTime = DateTime.Now,
                Success = true
            };
        }

        private string GenerateTailProcessData(string barcode, ScrewInstallationResult screwResult)
        {
            var processData = new
            {
                processName = "安装尾椎",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                barcodes = new[]
                {
                    new { codeType = "C", barcode = barcode }
                },
                testItems = new[]
                {
                    new { id = 37, itemName = "尾椎安装至C壳", itemType = 0, pass = true, resultText = "完成" },
                    new { id = 38, itemName = "螺丝扭矩检测", itemType = 1, pass = screwResult.Success, resultText = $"{screwResult.Torque}Nm" }
                }
            };

            return JsonConvert.SerializeObject(processData, Formatting.Indented);
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

            return JsonConvert.SerializeObject(allProcesses, Formatting.Indented);
        }

        public void EmergencyStop()
        {
            try
            {
                isRunning = false;
                LogManager.LogWarning("执行紧急停止");

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