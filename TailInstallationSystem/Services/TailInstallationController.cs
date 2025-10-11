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
        private CommunicationConfig _config;
        private readonly ConcurrentQueue<string> receivedProcessData = new ConcurrentQueue<string>();
        private const int MAX_PROCESS_DATA_COUNT = 10; // 添加队列大小限制
        private readonly object processDataLock = new object();
        private string _currentProductBarcode = null;
        private readonly object _barcodeCacheLock = new object();

        // 简化状态管理
        private bool isRunning = false;
        private readonly object runningStateLock = new object();

        // 工作模式
        private WorkMode _currentWorkMode = WorkMode.FullProcess;
        private readonly object _workModeLock = new object();

        // 条码等待任务管理
        private volatile TaskCompletionSource<string> barcodeWaitTask;
        private readonly object barcodeTaskLock = new object();

        // 扫码缓存机制
        private string cachedBarcode = null;
        private readonly object barcodeLock = new object();
        private CancellationTokenSource cancellationTokenSource;

        // 拧紧轴数据缓存
        private TighteningAxisData latestTighteningData = null;
        private readonly object tighteningDataLock = new object();

        #region Events
        public event Action<string, string> OnProcessStatusChanged;
        public event Action<string, string> OnCurrentProductChanged;
        #endregion

        public TailInstallationController(CommunicationManager communicationManager)
        {
            commManager = communicationManager;
            _config = commManager?.GetCurrentConfig() ?? ConfigManager.LoadConfig();
            dataManager = new DataManager(_config);
            cancellationTokenSource = new CancellationTokenSource();

            // 绑定事件
            commManager.OnDataReceived += ProcessReceivedData;
            commManager.OnBarcodeScanned += ProcessBarcodeData;
            commManager.OnTighteningDataReceived += ProcessTighteningData; 
        }
        /// <summary>
        /// 更新工作模式
        /// </summary>
        public void UpdateWorkMode(WorkMode newMode)
        {
            lock (_workModeLock)
            {
                if (_currentWorkMode != newMode)
                {
                    _currentWorkMode = newMode;
                    LogManager.LogInfo($"工作模式已切换为: {GetWorkModeDisplayName(newMode)}");
                }
            }
        }
        /// <summary>
        /// 获取当前工作模式
        /// </summary>
        public WorkMode GetCurrentWorkMode()
        {
            lock (_workModeLock)
            {
                return _currentWorkMode;
            }
        }
        /// <summary>
        /// 获取工作模式显示名称
        /// </summary>
        private string GetWorkModeDisplayName(WorkMode mode)
        {
            switch (mode)
            {
                case WorkMode.FullProcess:
                    return "完整流程模式";
                case WorkMode.Independent:
                    return "独立模式（仅工序4）";
                default:
                    return "未知模式";
            }
        }


        public async Task StartSystem()
        {
            var licenseManager = new LicenseManager();
            licenseManager.CheckActive();
            if (!licenseManager.ShowActive())
            {
                throw new InvalidOperationException("软件授权验证失败，无法启动系统");
            }

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
                // 清空条码缓存
                lock (_barcodeCacheLock)
                {
                    _currentProductBarcode = null;
                    LogManager.LogInfo("系统启动，已清空条码缓存");
                }

                // 清空扫码缓存
                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                // 清空工序数据队列
                lock (processDataLock)
                {
                    while (receivedProcessData.TryDequeue(out _)) { }
                    LogManager.LogInfo("系统启动，已清空工序数据队列");
                }

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

            try
            {
                LogManager.LogInfo("开始停止系统...");

                // 1. 取消所有异步操作
                cancellationTokenSource?.Cancel();

                // 2. 等待短暂时间让异步任务完成
                await Task.Delay(500);

                // 3. 先解绑事件，避免继续接收数据
                if (commManager != null)
                {
                    commManager.OnDataReceived -= ProcessReceivedData;
                    commManager.OnBarcodeScanned -= ProcessBarcodeData;
                    commManager.OnTighteningDataReceived -= ProcessTighteningData;
                }

                // 4. 清理等待任务
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

                // 5. 清空缓存
                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                lock (tighteningDataLock)
                {
                    latestTighteningData = null;
                }

                // 6. 清空处理数据队列
                lock (processDataLock)
                {
                    while (receivedProcessData.TryDequeue(out _)) { }
                }

                // 7. 释放通讯管理器
                commManager?.Dispose();
                
                LogManager.LogInfo("系统已完全停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止系统时发生异常: {ex.Message}");
            }
        }

        private async Task MainWorkLoop()
        {
            LogManager.LogInfo("主工作循环已启动");

            DateTime lastDebugOutput = DateTime.MinValue;
            bool isScanProcessing = false;      // 扫码处理中标志
            bool isTighteningProcessing = false; // 拧紧数据处理中标志

            while (GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    bool shouldOutputDebug = (now - lastDebugOutput).TotalSeconds >= 10;

                    if (shouldOutputDebug)
                    {
                        LogManager.LogDebug($"主循环检查 - 扫码中:{isScanProcessing}, 拧紧中:{isTighteningProcessing}");
                        lastDebugOutput = now;
                    }

                    if (!isScanProcessing)
                    {
                        try
                        {
                            bool scanTriggered = await commManager.CheckScanTrigger();
                            if (scanTriggered)
                            {
                                LogManager.LogInfo("D500触发：开始扫码流程");
                                isScanProcessing = true;
                                try
                                {
                                    await ExecuteScanProcess();
                                }
                                finally
                                {
                                    isScanProcessing = false;
                                }
                            }
                        }
                        catch (Exception scanEx)
                        {
                            LogManager.LogError($"扫码流程检测异常: {scanEx.Message}");
                            isScanProcessing = false;

                            if (scanEx.Message.Contains("远程主机强迫关闭") ||
                                scanEx.Message.Contains("目标计算机积极拒绝"))
                            {
                                commManager.SafeTriggerDeviceConnectionChanged("PLC", false);
                            }
                        }
                    }

                    if (!isTighteningProcessing)
                    {
                        try { 
                        bool tighteningTriggered = await commManager.CheckTighteningTrigger();

                        if (tighteningTriggered)
                        {
                            LogManager.LogInfo("D501触发：开始读取拧紧数据");
                            isTighteningProcessing = true;

                            try
                            {
                                await ExecuteTighteningDataProcess();
                            }
                            finally
                            {
                                isTighteningProcessing = false;
                            }
                        }
                    }
                        catch (Exception tightEx)
                        {
                            LogManager.LogError($"拧紧流程检测异常: {tightEx.Message}");
                            isTighteningProcessing = false;

                            // 如果是PLC通信异常，更新状态
                            if (tightEx.Message.Contains("远程主机强迫关闭") ||
                                tightEx.Message.Contains("目标计算机积极拒绝"))
                            {
                                commManager.SafeTriggerDeviceConnectionChanged("PLC", false);
                            }
                        }
                    }
                    await Task.Delay(50, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    LogManager.LogInfo("主工作循环被取消退出");
                    break;
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"主工作循环异常: {ex.Message}");
                    isScanProcessing = false;
                    isTighteningProcessing = false;
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }
            }

            LogManager.LogInfo("主工作循环已退出");
        }

        /// <summary>
        /// 执行扫码流程（D500触发）
        /// </summary>
        private async Task ExecuteScanProcess()
        {
            var startTime = DateTime.Now;
            string barcode = null;
            bool scanSuccess = false;

            try
            {
                LogManager.LogInfo("========== 扫码流程开始 ==========");
                OnProcessStatusChanged?.Invoke("", "开始扫码");
                lock (barcodeLock)
                {
                    if (!string.IsNullOrEmpty(cachedBarcode))
                    {
                        LogManager.LogInfo($"清空旧的扫码缓存: {cachedBarcode}");
                        cachedBarcode = null;
                    }
                }
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask != null)
                    {
                        try
                        {
                            if (!barcodeWaitTask.Task.IsCompleted)
                            {
                                barcodeWaitTask.TrySetCanceled();
                            }
                        }
                        catch { }
                        barcodeWaitTask = null;
                        LogManager.LogDebug("已清理旧的扫码等待任务");
                    }
                }

                if (!commManager.IsHeartbeatRunning())
                {
                    LogManager.LogWarning("心跳信号未运行，可能影响PLC通讯");
                }
                barcode = await WaitForBarcodeScanWithRetry();

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    scanSuccess = true;

                    lock (_barcodeCacheLock)
                    {
                        _currentProductBarcode = barcode;
                        LogManager.LogInfo($"条码已缓存: {barcode}");
                    }

                    OnCurrentProductChanged?.Invoke(barcode, "扫码成功");
                    LogManager.LogInfo($"扫码成功 | 条码:{barcode}");

                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.ScanResultAddress, 1);

                    LogManager.LogDebug("PLC反馈 | D520=1 (扫码OK)");
                }
                else
                {
                    scanSuccess = false;
                    LogManager.LogError("扫码失败 | 原因:条码为空");

                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D520=2 (扫码NG)");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"扫码流程异常 | 错误:{ex.Message}");

                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
            }
            finally
            {
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanTriggerAddress, 0);
                LogManager.LogDebug("PLC复位 | D500=0");

                var duration = (DateTime.Now - startTime).TotalSeconds;

                LogManager.LogInfo($"========== 扫码流程结束 | 条码:{barcode ?? "无"} | 结果:{(scanSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }


        /// <summary>
        /// 执行拧紧数据读取流程（D501触发）
        /// </summary>
        private async Task ExecuteTighteningDataProcess()
        {
            var startTime = DateTime.Now;
            bool dataGetSuccess = false; 
            string currentBarcode = null;

            try
            {
                LogManager.LogInfo("========== 拧紧流程开始 ==========");

                currentBarcode = GetCurrentBarcode();
                LogManager.LogInfo($"条码获取 | {currentBarcode}");

                OnProcessStatusChanged?.Invoke(currentBarcode, "读取拧紧数据");

                if (!commManager.IsHeartbeatRunning())
                {
                    LogManager.LogError("心跳信号未运行，扫码流程可能未正常执行");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "心跳异常，流程中断");

                    // 保存降级数据
                    await SaveFallbackData(currentBarcode, "心跳信号未运行");

                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D521=2 (心跳异常)");

                    return; // 提前退出
                }

                LogManager.LogInfo("心跳状态检查通过，继续拧紧流程");

                OnProcessStatusChanged?.Invoke(currentBarcode, "等待拧紧完成");
                var tighteningResult = await WaitForTighteningCompletion();

                if (tighteningResult != null)
                {
                    // 数据获取成功
                    dataGetSuccess = true;
                    OnProcessStatusChanged?.Invoke(currentBarcode, "拧紧数据获取成功");

                    // 日志明确区分合格/不合格
                    if (tighteningResult.Success)
                    {
                        LogManager.LogInfo($"拧紧合格 | 条码:{currentBarcode} | 扭矩:{tighteningResult.Torque:F2}Nm");
                    }
                    else
                    {
                        LogManager.LogWarning($"拧紧不合格 | 条码:{currentBarcode} | 原因:{tighteningResult.QualityResult}");
                    }

                    // 组装数据
                    var processDataArray = SafeGetProcessData();
                    var tailProcessData = GenerateTailProcessData(currentBarcode, tighteningResult);
                    var completeData = CombineAllProcessData(tailProcessData, processDataArray);

                    // 保存到数据库
                    await dataManager.SaveProductData(
                        currentBarcode, processDataArray, tailProcessData, completeData);

                    // 保存本地文件
                    await LocalFileManager.SaveProductionData(
                        currentBarcode,
                        processDataArray.Length > 0 ? processDataArray[0] : null,
                        processDataArray.Length > 1 ? processDataArray[1] : null,
                        processDataArray.Length > 2 ? processDataArray[2] : null,
                        tailProcessData
                    );

                    bool uploadSuccess = await dataManager.UploadToServer(currentBarcode, completeData);

                    LogManager.LogInfo($"数据已处理 | 条码:{currentBarcode} | 合格:{tighteningResult.Success} | 数据库:✓ | 文件:✓ | 上传:{(uploadSuccess ? "✓" : "✗")}");

                    // D521 = 1: 数据获取成功（无论合格与否）
                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.TighteningResultAddress, 1);
                    LogManager.LogDebug("PLC反馈 | D521=1 (数据获取成功)");
                }
                else
                {
                    // 数据获取失败（超时/通信异常）
                    dataGetSuccess = false;
                    LogManager.LogError($"拧紧数据获取失败 | 条码:{currentBarcode} | 原因:超时或通信异常");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "拧紧数据获取失败");
                    // 保存降级数据
                    await SaveFallbackData(currentBarcode, "拧紧数据获取超时或通信异常");
                    // D521 = 2: 数据获取失败
                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D521=2 (数据获取失败)");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧流程异常 | 错误:{ex.Message}");
                // 异常也保存降级数据
                await SaveFallbackData(currentBarcode ?? "UNKNOWN", $"流程异常: {ex.Message}");
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
            }
            finally
            {
                // 复位D501
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.TighteningTriggerAddress, 0);
                LogManager.LogDebug("PLC复位 | D501=0");
                // 清空条码缓存，准备下一个产品
                ClearBarcodeCache();
                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 拧紧流程结束 | 条码:{currentBarcode ?? "UNKNOWN"} | 数据获取:{(dataGetSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        /// <summary>
        /// 保存降级数据（数据获取失败时）
        /// </summary>
        private async Task SaveFallbackData(string barcode, string errorReason)
        {
            try
            {
                LogManager.LogWarning($"保存降级数据 | 条码:{barcode} | 原因:{errorReason}");

                // 生成降级的工序4数据
                var fallbackData = GenerateFallbackData(barcode, errorReason);

                // 获取前3道工序数据
                var processDataArray = SafeGetProcessData();

                // 合并完整数据
                var completeData = CombineAllProcessData(fallbackData, processDataArray);

                // 保存到数据库（标记为异常）
                await dataManager.SaveProductData(
                    barcode,
                    processDataArray,
                    fallbackData,  // 工序4为异常数据
                    completeData
                );

                // 保存到本地文件
                await LocalFileManager.SaveProductionData(
                    barcode,
                    processDataArray.Length > 0 ? processDataArray[0] : null,
                    processDataArray.Length > 1 ? processDataArray[1] : null,
                    processDataArray.Length > 2 ? processDataArray[2] : null,
                    fallbackData
                );

                LogManager.LogInfo($"降级数据保存成功 | 条码:{barcode}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存降级数据失败 | 条码:{barcode} | 错误:{ex.Message}");
            }
        }

        /// <summary>
        /// 生成降级数据（数据获取失败时的占位数据）
        /// </summary>
        private string GenerateFallbackData(string barcode, string errorReason)
        {
            var fallbackProcessData = new
            {
                ProcessId = "14",
                Code = barcode,
                Data = new[]
                {
            new
            {
                ItemName = "尾椎安装",
                Remark = $"扭矩：0Nm",
                Result = "NG"
            }
        }
            };

            return JsonConvert.SerializeObject(fallbackProcessData, Formatting.Indented);
        }

        /// <summary>
        /// 获取当前产品条码（从缓存中获取）
        /// </summary>
        private string GetCurrentBarcode()
        {
            string barcode;
            lock (_barcodeCacheLock)
            {
                barcode = _currentProductBarcode;
            }

            if (string.IsNullOrWhiteSpace(barcode))
            {
                LogManager.LogWarning("获取条码时缓存为空，返回UNKNOWN");
                return "UNKNOWN";
            }

            LogManager.LogDebug($"从缓存获取条码: {barcode}"); 
            return barcode;
        }

        /// <summary>
        /// 清空条码缓存（处理完成后调用）
        /// </summary>
        private void ClearBarcodeCache()
        {
            lock (_barcodeCacheLock)
            {
                LogManager.LogInfo($"清空条码缓存: {_currentProductBarcode}");
                _currentProductBarcode = null;
            }
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
            // 独立模式判断
            if (GetCurrentWorkMode() == WorkMode.Independent)
            {
                LogManager.LogInfo($"独立模式：已忽略前端发送的工序数据（大小:{jsonData?.Length ?? 0}字节）");
                return; // 直接丢弃数据
            }
            try
            {
                lock (processDataLock)
                {
                    // 限制队列大小
                    while (receivedProcessData.Count >= MAX_PROCESS_DATA_COUNT)
                    {
                        if (receivedProcessData.TryDequeue(out var oldData))
                        {
                            LogManager.LogWarning($"队列溢出 | 丢弃旧数据:{oldData.Length}字节");
                        }
                    }

                    receivedProcessData.Enqueue(jsonData);
                    int count = receivedProcessData.Count;

                    LogManager.LogInfo($"工序数据 | 序号:{count}/3 | 大小:{jsonData?.Length ?? 0}字节 | 队列:{count}条");

                    string statusMessage = "";
                    if (count == 1)
                    {
                        statusMessage = "已接收第一道工序数据";
                    }
                    else if (count == 2)
                    {
                        statusMessage = "已接收第二道工序数据";
                    }
                    else if (count == 3)
                    {
                        statusMessage = "已接收第三道工序数据（数据已收齐）";
                        int totalSize = receivedProcessData.Sum(d => d.Length);
                        LogManager.LogInfo($"前3道工序数据已收齐 | 总大小:{totalSize}字节 | 等待扫码触发");
                    }
                    else if (count > 3)
                    {
                        statusMessage = $"工序数据超过预期: {count}道";
                        LogManager.LogWarning(statusMessage);
                    }

                    OnProcessStatusChanged?.Invoke("", statusMessage);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理工序数据异常 | 错误:{ex.Message} | 数据长度:{jsonData?.Length ?? 0}");
                OnProcessStatusChanged?.Invoke("", $"数据处理异常");
            }
        }

        private void ProcessBarcodeData(string barcode)
        {
            try
            {
                LogManager.LogInfo($"扫描到条码: {barcode}");

                bool taskCompleted = false;

                lock (barcodeLock)
                {
                    cachedBarcode = barcode;
                    LogManager.LogDebug($"条码已缓存: {barcode}");
                }

                // 尝试完成等待任务
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                    {
                        try
                        {
                            taskCompleted = barcodeWaitTask.TrySetResult(barcode);

                            if (taskCompleted)
                            {
                                LogManager.LogDebug($"等待任务已完成 | 条码:{barcode}");
                                barcodeWaitTask = null;
                            }
                            else
                            {
                                LogManager.LogWarning($"等待任务设置失败 | 任务状态:{barcodeWaitTask.Task.Status}");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogError($"完成等待任务异常 | 错误:{ex.Message}");
                        }
                    }
                    else
                    {
                        if (barcodeWaitTask == null)
                        {
                            LogManager.LogDebug("无等待任务，条码已缓存");
                        }
                        else
                        {
                            LogManager.LogDebug($"等待任务已完成，状态:{barcodeWaitTask.Task.Status}");
                        }
                    }
                }

                // 触发界面更新事件
                try
                {
                    OnCurrentProductChanged?.Invoke(barcode, "已扫描条码");
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"触发界面更新事件异常: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理条码数据异常: {ex.Message} | 堆栈:{ex.StackTrace}");
            }
        }

        // 拧紧轴数据处理方法 
        private void ProcessTighteningData(TighteningAxisData tighteningData)
        {
            try
            {
                lock (tighteningDataLock)
                {
                    latestTighteningData = tighteningData;
                }

                if (tighteningData.IsOperationCompleted)
                {
                    LogManager.LogInfo($"拧紧操作完成 - 扭矩: {tighteningData.CompletedTorque:F2}Nm, 结果: {tighteningData.QualityResult}");
                }
                else if (tighteningData.IsRunning)
                {
                    LogManager.LogInfo($"拧紧轴运行中 - 实时扭矩: {tighteningData.RealtimeTorque:F2}Nm, 目标: {tighteningData.TargetTorque:F2}Nm");
                }

                if (tighteningData.HasError)
                {
                    LogManager.LogError($"拧紧轴错误 - 错误代码: {tighteningData.ErrorCode}, 状态: {tighteningData.GetStatusDisplayName()}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理拧紧轴数据异常: {ex.Message}");
            }
        }


        private string[] SafeGetProcessData()
        {
            // 独立模式判断
            if (GetCurrentWorkMode() == WorkMode.Independent)
            {
                LogManager.LogInfo("独立模式：跳过前3道工序数据获取");
                return new string[0]; // 返回空数组
            }
            lock (processDataLock)
            {
                var processDataArray = new string[3]; // 固定长度3
                var tempList = receivedProcessData.ToList();

                int availableCount = Math.Min(3, tempList.Count);
                for (int i = 0; i < availableCount; i++)
                {
                    processDataArray[i] = tempList[i];
                }

                // 移除已使用的数据
                for (int i = 0; i < availableCount; i++)
                {
                    receivedProcessData.TryDequeue(out _);
                }

                LogManager.LogInfo($"获取工序数据: 有效{availableCount}条, 剩余队列: {receivedProcessData.Count}条");

                return processDataArray;
            }
        }


        /// <summary>
        /// 持续发送"ON"指令直到扫码成功
        /// </summary>
        private async Task<string> WaitForBarcodeScanWithRetry()
        {
            LogManager.LogInfo("开始持续扫码流程...");

            int retryCount = 0;
            const int maxRetries = 12; // 最大重试次数（避免无限循环）
            const int scanInterval = 5000; // 每5秒重试一次

            while (retryCount < maxRetries && GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    retryCount++;

                    // 步骤1：发送扫码指令
                    OnProcessStatusChanged?.Invoke("", $"发送扫码指令 (第{retryCount}次)");
                    LogManager.LogInfo($"扫码指令 | 尝试:{retryCount}/{maxRetries}");

                    bool scanCommandSent = await commManager.SendScannerCommand("ON");
                    if (!scanCommandSent)
                    {
                        LogManager.LogWarning($"指令发送失败 | 尝试:{retryCount}/{maxRetries} | {scanInterval / 1000}秒后重试");
                        OnProcessStatusChanged?.Invoke("", $"扫码枪通信失败，{scanInterval / 1000}秒后重试");

                        // 等待后继续重试
                        await Task.Delay(scanInterval, cancellationTokenSource.Token);
                        continue;
                    }

                    // 步骤2：等待条码扫描（较短超时）
                    OnProcessStatusChanged?.Invoke("", $"等待条码扫描... (第{retryCount}次)");

                    try
                    {
                        string barcode = await WaitForBarcodeScanSingle(scanInterval); // 5秒超时

                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            LogManager.LogInfo($"扫码成功 | 条码:{barcode} | 尝试次数:{retryCount}");
                            OnProcessStatusChanged?.Invoke("", "条码扫描成功");
                            return barcode;
                        }
                    }
                    catch (TimeoutException)
                    {
                        LogManager.LogDebug($"扫码超时 | 尝试:{retryCount}/{maxRetries} | 继续重试");
                        OnProcessStatusChanged?.Invoke("", $"扫码超时，{scanInterval / 1000}秒后重试 (第{retryCount}/{maxRetries}次)");
                    }

                    // 检查系统是否被取消
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        LogManager.LogInfo("扫码流程被取消");
                        throw new OperationCanceledException("扫码流程被用户取消");
                    }

                    // 短暂等待后继续下一次尝试
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    LogManager.LogInfo("扫码重试流程被取消");
                    throw;
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"扫码尝试异常 | 尝试:{retryCount}/{maxRetries} | 错误:{ex.Message}");

                    // 异常情况下也要等待一段时间
                    await Task.Delay(2000, cancellationTokenSource.Token);
                }
            }

            // 达到最大重试次数
            LogManager.LogError($"扫码重试达到最大次数 ({maxRetries})，放弃扫码");
            OnProcessStatusChanged?.Invoke("", $"扫码失败，已重试{maxRetries}次");
            throw new TimeoutException($"扫码重试达到最大次数 ({maxRetries})");
        }

        /// <summary>
        /// 单次扫码等待（短超时）
        /// </summary>
        private async Task<string> WaitForBarcodeScanSingle(int timeoutMs = 5000)
        {
            // 先检查缓存
            lock (barcodeLock)
            {
                if (!string.IsNullOrEmpty(cachedBarcode))
                {
                    LogManager.LogDebug($"使用缓存条码: {cachedBarcode}");
                    string result = cachedBarcode;
                    cachedBarcode = null;
                    return result;
                }
            }

            // 创建等待任务
            TaskCompletionSource<string> waitTask;
            lock (barcodeTaskLock)
            {
                barcodeWaitTask = new TaskCompletionSource<string>();
                waitTask = barcodeWaitTask;
            }

            try
            {
                using (var timeoutCts = new CancellationTokenSource(timeoutMs))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, cancellationTokenSource.Token))
                {
                    var timeoutTask = Task.Delay(timeoutMs, combinedCts.Token);
                    var completedTask = await Task.WhenAny(waitTask.Task, timeoutTask);

                    if (completedTask == waitTask.Task && !waitTask.Task.IsCanceled)
                    {
                        LogManager.LogDebug($"等待任务完成，条码: {waitTask.Task.Result}");
                        return waitTask.Task.Result;
                    }
                    else
                    {
                        await Task.Delay(200); // 给200ms缓冲时间

                        lock (barcodeLock)
                        {
                            if (!string.IsNullOrEmpty(cachedBarcode))
                            {
                                LogManager.LogInfo($"超时后发现缓存条码: {cachedBarcode}");
                                string result = cachedBarcode;
                                cachedBarcode = null;
                                return result;
                            }
                        }

                        LogManager.LogDebug($"单次扫码超时 ({timeoutMs}ms)");
                        throw new TimeoutException($"单次扫码超时 ({timeoutMs}ms)");
                    }
                }
            }
            catch (OperationCanceledException)
            {
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


        // 等待拧紧操作完成方法 
        private async Task<TighteningResult> WaitForTighteningCompletion()
        {
            LogManager.LogInfo("等待拧紧轴操作完成...");

            var config = commManager.GetCurrentConfig();
            var maxTimeout = config.TighteningAxis.MaxOperationTimeoutSeconds;

            // 使用通信管理器的等待方法
            var tighteningData = await commManager.WaitForTighteningCompletion(maxTimeout);

            if (tighteningData != null)
            {
                LogManager.LogInfo($"拧紧操作完成 - 扭矩: {tighteningData.CompletedTorque:F2}Nm, 结果: {tighteningData.QualityResult}");

                return new TighteningResult
                {
                    Torque = tighteningData.CompletedTorque,                  
                    TargetTorque = tighteningData.TargetTorque,                
                    LowerLimitTorque = tighteningData.LowerLimitTorque,         
                    UpperLimitTorque = tighteningData.UpperLimitTorque,      
                    TighteningTime = tighteningData.Timestamp,
                    Success = tighteningData.IsQualified,
                    QualityResult = tighteningData.QualityResult,
                    ErrorCode = tighteningData.ErrorCode,
                    StatusCode = tighteningData.RunningStatusCode,
                    QualifiedCount = tighteningData.QualifiedCount,
                    TorqueAchievementRate = tighteningData.TorqueAchievementRate
                };
            }
            else
            {
                LogManager.LogError("等待拧紧操作完成超时或失败");

                // 尝试获取最后的拧紧数据
                var lastData = await commManager.ReadTighteningAxisData();

                return new TighteningResult
                {
                    Torque = lastData?.CompletedTorque ?? 0f,                  
                    TargetTorque = lastData?.TargetTorque ?? 0f,              
                    LowerLimitTorque = lastData?.LowerLimitTorque ?? 0f,      
                    UpperLimitTorque = lastData?.UpperLimitTorque ?? 0f,      
                    TighteningTime = DateTime.Now,
                    Success = false,
                    QualityResult = "操作超时或通信异常",
                    ErrorCode = lastData?.ErrorCode ?? -1,
                    StatusCode = lastData?.RunningStatusCode ?? -1
                };
            }
        }

        private string GenerateTailProcessData(string barcode, TighteningResult tighteningResult)
        {
            LogManager.LogInfo($"生成尾椎安装工序数据: {barcode}");

            var processData = new
            {
                ProcessId = "14",
                Code = barcode,
                Data = new[]
                {
            new
            {
                ItemName = "尾椎安装",
                Remark = $"扭矩：{tighteningResult.Torque:F2}Nm",
                Result = tighteningResult.Success ? "PASS" : "NG"
            }
        }
            };

            string jsonResult = JsonConvert.SerializeObject(processData, Formatting.Indented);

            LogManager.LogDebug($"工序4数据生成 | 条码:{barcode} | 扭矩:{tighteningResult.Torque:F2}Nm | 结果:{(tighteningResult.Success ? "PASS" : "NG")}");

            return jsonResult;
        }


        private string CombineAllProcessData(string tailProcessData, string[] processDataArray)
        {
            // 独立模式判断
            if (GetCurrentWorkMode() == WorkMode.Independent)
            {
                LogManager.LogInfo("独立模式：仅上传工序4数据");

                var independentData = new List<object>();

                // 添加工序4数据
                if (!string.IsNullOrEmpty(tailProcessData))
                {
                    try
                    {
                        independentData.Add(JsonConvert.DeserializeObject(tailProcessData));
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"工序4数据解析失败 | 错误:{ex.Message}");
                        independentData.Add(new { error = "工序4数据解析失败", originalData = tailProcessData });
                    }
                }
                else
                {
                    independentData.Add(new { error = "工序4数据为空" });
                }
                string independentJson = JsonConvert.SerializeObject(independentData, Formatting.Indented);
                LogManager.LogInfo($"独立模式数据合并完成 | 工序4:{tailProcessData?.Length ?? 0}B | 总计:{independentJson.Length}B");

                return independentJson;  
            }

            var allProcesses = new List<object>();
            int[] sizes = new int[4]; // 记录每道工序的数据大小

            // 添加前三道工序数据
            for (int i = 0; i < processDataArray.Length; i++)
            {
                if (!string.IsNullOrEmpty(processDataArray[i]))
                {
                    try
                    {
                        allProcesses.Add(JsonConvert.DeserializeObject(processDataArray[i]));
                        sizes[i] = processDataArray[i].Length;
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"工序{i + 1}数据解析失败 | 错误:{ex.Message}");
                        allProcesses.Add(new { error = "数据解析失败", originalData = processDataArray[i] });
                        sizes[i] = 0; // 标记为异常
                    }
                }
                else
                {
                    // 数据为空的情况
                    allProcesses.Add(new { error = "数据为空" });
                    sizes[i] = 0;
                    LogManager.LogWarning($"工序{i + 1}数据为空");
                }
            }

            // 添加尾椎安装数据（工序4）
            if (!string.IsNullOrEmpty(tailProcessData))
            {
                try
                {
                    allProcesses.Add(JsonConvert.DeserializeObject(tailProcessData));
                    sizes[3] = tailProcessData.Length;
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"工序4数据解析失败 | 错误:{ex.Message}");
                    allProcesses.Add(new { error = "尾椎数据解析失败", originalData = tailProcessData });
                    sizes[3] = 0;
                }
            }
            else
            {
                allProcesses.Add(new { error = "尾椎数据为空" });
                sizes[3] = 0;
                LogManager.LogWarning("工序4数据为空");
            }

            string combinedData = JsonConvert.SerializeObject(allProcesses, Formatting.Indented);

            LogManager.LogInfo($"数据合并 | " +
                              $"工序1:{sizes[0]}B | " +
                              $"工序2:{sizes[1]}B | " +
                              $"工序3:{sizes[2]}B | " +
                              $"工序4:{sizes[3]}B | " +
                              $"总计:{combinedData.Length}B");

            return combinedData;
        }


        public void EmergencyStop()
        {
            try
            {
                LogManager.LogWarning("执行紧急停止");
                
                lock (runningStateLock)
                {
                    isRunning = false;
                }

                // 1. 立即取消所有异步操作
                cancellationTokenSource?.Cancel();

                // 2. 先解绑事件，避免继续接收数据
                if (commManager != null)
                {
                    commManager.OnDataReceived -= ProcessReceivedData;
                    commManager.OnBarcodeScanned -= ProcessBarcodeData;
                    commManager.OnTighteningDataReceived -= ProcessTighteningData;
                }

                // 3. 清理等待任务
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

                // 4. 清空缓存
                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                lock (tighteningDataLock)
                {
                    latestTighteningData = null;
                }

                // 5. 清空处理数据队列
                lock (processDataLock)
                {
                    while (receivedProcessData.TryDequeue(out _)) { }
                }

                // 6. 等待短暂时间让事件处理完成
                System.Threading.Thread.Sleep(100);

                // 7. 最后释放通讯管理器
                commManager?.Dispose();
                
                LogManager.LogWarning("系统已紧急停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"紧急停止异常: {ex.Message}");
            }
        }

        public void UpdateCommunicationManager(CommunicationManager newCommManager)
        {
            try
            {
                LogManager.LogInfo("更新控制器的通讯管理器引用");

                // 解绑旧事件
                if (commManager != null)
                {
                    commManager.OnDataReceived -= ProcessReceivedData;
                    commManager.OnBarcodeScanned -= ProcessBarcodeData;
                    commManager.OnTighteningDataReceived -= ProcessTighteningData;
                }

                // 更新引用
                commManager = newCommManager;

                // 绑定新事件
                if (commManager != null)
                {
                    commManager.OnDataReceived += ProcessReceivedData;
                    commManager.OnBarcodeScanned += ProcessBarcodeData;
                    commManager.OnTighteningDataReceived += ProcessTighteningData;
                }

                LogManager.LogInfo("控制器通讯管理器更新完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新通讯管理器失败: {ex.Message}");
            }
        }


        // 拧紧结果类 
        public class TighteningResult
        {
            public float Torque { get; set; }
            public float TargetTorque { get; set; }
            public float LowerLimitTorque { get; set; }
            public float UpperLimitTorque { get; set; }
            public DateTime TighteningTime { get; set; }
            public bool Success { get; set; }
            public string QualityResult { get; set; }
            public int ErrorCode { get; set; }
            public int StatusCode { get; set; }
            public int QualifiedCount { get; set; }
            public double TorqueAchievementRate { get; set; }
        }

    }
}
