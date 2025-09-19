using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public class TailInstallationController
    {
        private CommunicationManager commManager;
        private DataManager dataManager;
        private readonly ConcurrentQueue<string> receivedProcessData = new ConcurrentQueue<string>();
        private const int MAX_PROCESS_DATA_COUNT = 10; // 添加队列大小限制
        private readonly object processDataLock = new object();

        // 简化状态管理
        private bool isRunning = false;
        private readonly object runningStateLock = new object();

        // 条码等待任务管理
        private volatile TaskCompletionSource<string> barcodeWaitTask;
        private readonly object barcodeTaskLock = new object();

        // 扫码缓存机制
        private string cachedBarcode = null;
        private readonly object barcodeLock = new object();
        private CancellationTokenSource cancellationTokenSource;
        #region Events
        public event Action<string, string> OnProcessStatusChanged;
        public event Action<string, string> OnCurrentProductChanged;
        #endregion
        public TailInstallationController(CommunicationManager communicationManager)
        {
            commManager = communicationManager;
            var config = commManager?.GetCurrentConfig() ?? ConfigManager.LoadConfig();
            dataManager = new DataManager(config);
            cancellationTokenSource = new CancellationTokenSource();

            // 绑定事件
            commManager.OnDataReceived += ProcessReceivedData;
            commManager.OnBarcodeScanned += ProcessBarcodeData;
            commManager.OnScrewDataReceived += ProcessScrewData;
        }

        public async Task StartSystem()
        {
            lock (runningStateLock)
            {
                if (isRunning)
                {
                    LogManager.LogWarning("系统已在运行中");
                    return;
                }
                isRunning = true;
            }

            try
            {
                await commManager.InitializeConnections();

                // 重新创建取消令牌
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                // 启动主工作循环
                _ = Task.Run(MainWorkLoop, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                lock (runningStateLock)
                {
                    isRunning = false;
                }
                throw;
            }
        }
        public async Task StopSystem()
        {
            lock (runningStateLock)
            {
                if (!isRunning)
                {
                    LogManager.LogWarning("系统未在运行");
                    return;
                }
                isRunning = false;
            }

            // 取消所有异步操作
            cancellationTokenSource?.Cancel();

            // 清理等待任务
            lock (barcodeTaskLock)
            {
                if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                {
                    try
                    {
                        barcodeWaitTask.SetCanceled();
                    }
                    catch (InvalidOperationException)
                    {
                        // 任务可能已经完成，忽略异常
                    }
                    barcodeWaitTask = null;
                }
            }

            // 清空缓存
            lock (barcodeLock)
            {
                cachedBarcode = null;
            }

            // 清空处理数据队列
            lock (processDataLock)
            {
                while (receivedProcessData.TryDequeue(out _)) { }
            }

            commManager?.Dispose();
        }

        private async Task MainWorkLoop()
        {
            while (GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
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
                    await Task.Delay(100, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"主工作循环异常: {ex.Message}");

                    // 发生异常时短暂等待，避免紧密循环
                    try
                    {
                        await Task.Delay(1000, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            LogManager.LogInfo("主工作循环已退出");
        }

        private bool GetRunningState()
        {
            lock (runningStateLock)
            {
                return isRunning;
            }
        }

        private void ProcessReceivedData(string jsonData)
        {
            try
            {
                lock (processDataLock)
                {
                    // 限制队列大小，防止内存无限增长
                    while (receivedProcessData.Count >= MAX_PROCESS_DATA_COUNT)
                    {
                        if (receivedProcessData.TryDequeue(out var oldData))
                        {
                            LogManager.LogWarning($"丢弃过期工序数据: {oldData.Substring(0, Math.Min(30, oldData.Length))}...");
                        }
                    }

                    receivedProcessData.Enqueue(jsonData);
                    LogManager.LogInfo($"接收到工序数据: {jsonData.Substring(0, Math.Min(50, jsonData.Length))}...");

                    int count = receivedProcessData.Count;
                    OnProcessStatusChanged?.Invoke("", $"已接收 {count}/3 道工序数据");

                    if (count >= 3)
                    {
                        LogManager.LogInfo("前三道工序数据已收齐，准备执行尾椎安装");
                        OnProcessStatusChanged?.Invoke("", "工序数据已收齐，等待PLC触发");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理接收数据异常: {ex.Message}");
                OnProcessStatusChanged?.Invoke("", $"数据处理异常: {ex.Message}");
            }
        }
        private bool ValidateProcessData()
        {
            lock (processDataLock)
            {
                int count = receivedProcessData.Count;
                if (count < 3)
                {
                    LogManager.LogWarning($"工序数据不足: 需要3道，当前{count}道");
                    return false;
                }

                // 验证数据格式
                var tempArray = receivedProcessData.ToArray();
                for (int i = 0; i < Math.Min(3, tempArray.Length); i++)
                {
                    if (string.IsNullOrWhiteSpace(tempArray[i]))
                    {
                        LogManager.LogWarning($"第{i + 1}道工序数据为空");
                        return false;
                    }

                    // 简单的JSON格式验证
                    try
                    {
                        JsonConvert.DeserializeObject(tempArray[i]);
                    }
                    catch (JsonException)
                    {
                        LogManager.LogWarning($"第{i + 1}道工序数据JSON格式无效");
                        return false;
                    }
                }

                return true;
            }
        }

        private string[] SafeGetProcessData()
        {
            lock (processDataLock)
            {
                var processDataArray = new string[Math.Min(3, receivedProcessData.Count)];
                for (int i = 0; i < processDataArray.Length; i++)
                {
                    if (!receivedProcessData.TryDequeue(out processDataArray[i]))
                    {
                        LogManager.LogWarning($"获取第{i + 1}道工序数据失败");
                        //processDataArray[i] = null;
                    }
                }
                return processDataArray;
            }
        }


        private void ProcessBarcodeData(string barcode)
        {
            lock (barcodeLock)
            {
                LogManager.LogInfo($"扫描到条码: {barcode}");

                // 只有在需要时才缓存
                TaskCompletionSource<string> currentWaitTask = null;
                lock (barcodeTaskLock)
                {
                    currentWaitTask = barcodeWaitTask;
                }
                if (currentWaitTask != null && !currentWaitTask.Task.IsCompleted)
                {
                    // 有等待任务，直接完成任务
                    LogManager.LogInfo("条码扫描等待任务已完成");
                    try
                    {
                        currentWaitTask.SetResult(barcode);
                        lock (barcodeTaskLock)
                        {
                            if (barcodeWaitTask == currentWaitTask)
                            {
                                barcodeWaitTask = null;
                            }
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        LogManager.LogError($"完成等待任务异常: {ex.Message}");
                    }
                }
                else
                {
                    // 没有等待任务，缓存条码
                    cachedBarcode = barcode;
                    LogManager.LogInfo($"条码已缓存: {barcode}");
                }

                OnCurrentProductChanged?.Invoke(barcode, "已扫描条码");
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
                // 验证工序数据完整性
                if (!ValidateProcessData())
                {
                    LogManager.LogWarning("工序数据验证失败，无法执行尾椎安装");
                    OnProcessStatusChanged?.Invoke("", "工序数据不完整或无效");
                    return;
                }
                LogManager.LogInfo("开始执行尾椎安装工序");
                OnProcessStatusChanged?.Invoke("", "开始执行尾椎安装");
                // 1. 等待条码扫描
                OnProcessStatusChanged?.Invoke("", "等待条码扫描");
                string barcode = await WaitForBarcodeScan();

                // 验证条码格式
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    throw new InvalidOperationException("扫描到的条码为空");
                }
                OnCurrentProductChanged?.Invoke(barcode, "等待螺丝安装");
                // 2. 执行螺丝安装
                OnProcessStatusChanged?.Invoke(barcode, "执行螺丝安装");
                var screwResult = await PerformScrewInstallation();
                OnCurrentProductChanged?.Invoke(barcode, "螺丝安装完成");
                // 3. 生成本工序数据
                OnProcessStatusChanged?.Invoke(barcode, "生成工序数据");
                var tailProcessData = GenerateTailProcessData(barcode, screwResult);
                // 4. 整合所有数据
                OnProcessStatusChanged?.Invoke(barcode, "整合数据");

                // 安全获取并验证工序数据
                var processDataArray = SafeGetProcessData();

                var completeData = CombineAllProcessData(tailProcessData, processDataArray);
                // 5. 保存到本地数据库
                OnProcessStatusChanged?.Invoke(barcode, "保存数据");
                await dataManager.SaveProductData(barcode, processDataArray, tailProcessData, completeData);
                // 6. 保存到本地JSON文件
                OnProcessStatusChanged?.Invoke(barcode, "保存本地文件");
                var localFileSaved = await LocalFileManager.SaveProductionData(
                    barcode,
                    processDataArray.Length > 0 ? processDataArray[0] : null,
                    processDataArray.Length > 1 ? processDataArray[1] : null,
                    processDataArray.Length > 2 ? processDataArray[2] : null,
                    tailProcessData
                );
                if (localFileSaved)
                {
                    LogManager.LogInfo($"产品 {barcode} 本地文件保存成功");
                }
                else
                {
                    LogManager.LogWarning($"产品 {barcode} 本地文件保存失败");
                }
                // 7. 上传到服务器
                OnProcessStatusChanged?.Invoke(barcode, "上传数据");
                bool uploadSuccess = await dataManager.UploadToServer(barcode, completeData);
                if (uploadSuccess)
                {
                    LogManager.LogInfo($"产品 {barcode} 数据上传成功");
                    OnCurrentProductChanged?.Invoke(barcode, "数据上传成功");
                }
                else
                {
                    LogManager.LogWarning($"产品 {barcode} 数据上传失败，已加入重试队列");
                    OnCurrentProductChanged?.Invoke(barcode, "数据上传失败");
                }
                LogManager.LogInfo("尾椎安装工序完成");
                OnProcessStatusChanged?.Invoke(barcode, "尾椎安装完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"尾椎安装异常: {ex.Message}");
                OnProcessStatusChanged?.Invoke("", $"安装异常: {ex.Message}");
            }
            finally
            {
                OnProcessStatusChanged?.Invoke("", "等待下一个产品");
            }
        }

        private async Task<string> WaitForBarcodeScan()
        {
            LogManager.LogInfo("等待条码扫描...");

            lock (barcodeLock)
            {
                // 检查是否有缓存的条码
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
            TaskCompletionSource<string> waitTask;
            lock (barcodeTaskLock)
            {
                barcodeWaitTask = new TaskCompletionSource<string>();
                waitTask = barcodeWaitTask;
            }
            try
            {
                // 设置超时（30秒）
                using (var timeoutCts = new CancellationTokenSource(30000))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, cancellationTokenSource.Token))
                {
                    var timeoutTask = Task.Delay(30000, combinedCts.Token);
                    var completedTask = await Task.WhenAny(waitTask.Task, timeoutTask);

                    if (completedTask == waitTask.Task && !waitTask.Task.IsCanceled)
                    {
                        LogManager.LogInfo("条码扫描完成");
                        return waitTask.Task.Result;
                    }
                    else
                    {
                        LogManager.LogError("等待条码扫描超时（30秒）");
                        throw new TimeoutException("等待条码扫描超时");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("条码扫描等待被取消");
                throw;
            }
            finally
            {
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask == waitTask)
                    {
                        barcodeWaitTask = null;
                    }
                }
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

        private string CombineAllProcessData(string tailProcessData, string[] processDataArray)
        {
            var allProcesses = new List<object>();
            // 添加前三道工序数据
            foreach (var processJson in processDataArray)
            {
                if (!string.IsNullOrEmpty(processJson))
                {
                    try
                    {
                        allProcesses.Add(JsonConvert.DeserializeObject(processJson));
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"解析工序数据失败: {ex.Message}");
                        allProcesses.Add(new { error = "数据解析失败", originalData = processJson });
                    }
                }
            }
            // 添加尾椎安装数据
            if (!string.IsNullOrEmpty(tailProcessData))
            {
                try
                {
                    allProcesses.Add(JsonConvert.DeserializeObject(tailProcessData));
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"解析尾椎安装数据失败: {ex.Message}");
                    allProcesses.Add(new { error = "尾椎数据解析失败", originalData = tailProcessData });
                }
            }
            string combinedData = JsonConvert.SerializeObject(allProcesses, Formatting.Indented);
            LogManager.LogInfo($"合并所有工序数据完成，总共 {allProcesses.Count} 道工序");
            return combinedData;
        }

        public void EmergencyStop()
        {
            try
            {
                lock (runningStateLock)
                {
                    isRunning = false;
                }
                LogManager.LogWarning("执行紧急停止");

                // 取消所有操作
                cancellationTokenSource?.Cancel();

                // 清理等待任务
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                    {
                        try
                        {
                            barcodeWaitTask.SetCanceled();
                        }
                        catch (InvalidOperationException)
                        {
                            // 任务可能已经完成，忽略异常
                        }
                        barcodeWaitTask = null;
                    }
                }

                // 清空缓存
                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                // 正确清空队列
                lock (processDataLock)
                {
                    while (receivedProcessData.TryDequeue(out _)) { }
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

        public class ScrewInstallationResult
        {
            public decimal Torque { get; set; }
            public DateTime InstallTime { get; set; }
            public bool Success { get; set; }
        }
    }
}