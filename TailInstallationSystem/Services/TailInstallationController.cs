using AntdUI;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    /// <summary>
    /// 尾椎安装控制器 
    /// </summary>
    public class TailInstallationController
    {
        private CommunicationManager commManager;
        private DataManager dataManager;
        private CommunicationConfig _config;

        // 扫码等待配置
        private const int SCAN_WAIT_TIMEOUT_SECONDS = 3;
        private const int SCAN_WAIT_CHECK_INTERVAL_MS = 500;
        private string _currentProductBarcode = null;
        private readonly object _barcodeCacheLock = new object();

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
        private CancellationTokenSource _currentMonitoringCts = null; 
        private readonly object _monitoringCtsLock = new object();

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

            if (commManager != null)
            {
                commManager.OnBarcodeScanned += ProcessBarcodeData;
                commManager.OnTighteningDataReceived += ProcessTighteningData;
                LogManager.LogInfo("控制器已订阅通讯事件");
            }
            else
            {
                LogManager.LogError("通讯管理器为null，无法订阅事件");
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
                await Task.Delay(200);
                LogManager.LogInfo("系统启动前等待资源就绪（200ms）");

                // 清空条码缓存
                lock (_barcodeCacheLock)
                {
                    _currentProductBarcode = null;
                    LogManager.LogInfo("系统启动，已清空条码缓存");
                }

                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                // 重新创建取消令牌
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                LogManager.LogInfo("=============");
                LogManager.LogInfo("系统已启动");
                LogManager.LogInfo("=============");

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

                // 先取消当前监控任务（如果存在）
                lock (_monitoringCtsLock)
                {
                    if (_currentMonitoringCts != null && !_currentMonitoringCts.IsCancellationRequested)
                    {
                        string currentBarcode = _currentProductBarcode ?? "未知产品";
                        LogManager.LogInfo($"停止系统：取消当前监控任务 | 条码:{currentBarcode}");
                        _currentMonitoringCts.Cancel();
                        _currentMonitoringCts = null; // 清空引用
                    }
                }

                // 取消系统主令牌
                cancellationTokenSource?.Cancel();
                await Task.Delay(500);

                // 清理等待任务
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                    {
                        try
                        {
                            barcodeWaitTask.SetCanceled();
                        }
                        catch (InvalidOperationException) { }
                        barcodeWaitTask = null;
                    }
                }

                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                lock (tighteningDataLock)
                {
                    latestTighteningData = null;
                }

                await Task.Delay(200);
                //commManager?.Dispose();
                await Task.Delay(300);

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
            bool isScanProcessing = false;

            while (GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    bool shouldOutputDebug = (now - lastDebugOutput).TotalSeconds >= 10;

                    if (shouldOutputDebug)
                    {
                        LogManager.LogDebug($"主循环检查 - 扫码中:{isScanProcessing}");
                        lastDebugOutput = now;
                    }

                    // 检查扫码触发（D500）
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
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }
            }

            LogManager.LogInfo("主工作循环已退出");
        }

        /// <summary>
        /// 扫码流程（D500触发）
        /// </summary>
        private async Task ExecuteScanProcess()
        {
            var startTime = DateTime.Now;
            string barcode = null;
            bool scanSuccess = false;
            try
            {
                LogManager.LogInfo("========== 扫码流程开始 ==========");
                OnProcessStatusChanged?.Invoke("", "扫码触发");
                // 清空旧缓存
                lock (barcodeLock)
                {
                    if (!string.IsNullOrEmpty(cachedBarcode))
                    {
                        LogManager.LogInfo($"清空旧的扫码缓存: {cachedBarcode}");
                        cachedBarcode = null;
                    }
                }
                // 清理等待任务
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
                    }
                }
                // 执行扫码
                barcode = await WaitForBarcodeScanWithRetry();
                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    scanSuccess = true;
                    // 取消旧的监控任务 
                    lock (_monitoringCtsLock)
                    {
                        // 检查是否有旧条码正在监控
                        if (!string.IsNullOrEmpty(_currentProductBarcode) && _currentProductBarcode != barcode)
                        {
                            LogManager.LogWarning($"⚠ 新产品扫码 | 旧产品:{_currentProductBarcode} 监控将被取消 | 新产品:{barcode}");

                            // 取消旧监控
                            if (_currentMonitoringCts != null && !_currentMonitoringCts.IsCancellationRequested)
                            {
                                _currentMonitoringCts.Cancel();
                                LogManager.LogInfo($"✓ 已取消旧产品监控任务");
                            }
                        }

                        // 创建新的取消令牌
                        _currentMonitoringCts = new CancellationTokenSource();
                    }
                    // 缓存条码
                    lock (_barcodeCacheLock)
                    {
                        _currentProductBarcode = barcode;
                        LogManager.LogInfo($"条码已缓存: {barcode}");
                    }
                    OnCurrentProductChanged?.Invoke(barcode, "扫码成功");
                    OnProcessStatusChanged?.Invoke(barcode, "扫码成功 - 开始监控拧紧");
                    LogManager.LogInfo($"扫码成功 | 条码:{barcode}");
                    // 反馈PLC扫码成功（D520=1）
                    await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanResultAddress, 1, "扫码OK");
                    //使用新的取消令牌启动监控 
                    CancellationTokenSource currentCts;
                    lock (_monitoringCtsLock)
                    {
                        currentCts = _currentMonitoringCts;
                    }

                    _ = Task.Run(() => MonitorTighteningCompletion(barcode, currentCts.Token), currentCts.Token);
                    LogManager.LogInfo($"已启动拧紧轴监控任务 | 条码:{barcode}");
                }
                else
                {
                    scanSuccess = false;
                    LogManager.LogError("扫码失败 | 原因:条码为空");

                    // 反馈PLC扫码失败（D520=2）
                    await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanResultAddress, 2, "扫码失败");
                }
            }
            catch (TimeoutException timeoutEx)
            {
                // 专门处理超时异常
                scanSuccess = false;
                LogManager.LogError($"扫码超时 | 错误:{timeoutEx.Message}");
                OnProcessStatusChanged?.Invoke("", "扫码超时 - 已达最大重试次数");

                // 反馈PLC扫码失败（D520=2）+ 记录日志
                await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanResultAddress, 2, "扫码超时");
            }
            catch (OperationCanceledException cancelEx)
            {
                scanSuccess = false;
                LogManager.LogWarning($"扫码流程被取消 | 原因:{cancelEx.Message}");
                OnProcessStatusChanged?.Invoke("", "扫码已取消");

                // 反馈PLC扫码失败（D520=2）
                await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanResultAddress, 2, "扫码取消");
            }
            catch (Exception ex)
            {
                scanSuccess = false;
                LogManager.LogError($"扫码流程异常 | 类型:{ex.GetType().Name} | 错误:{ex.Message}");
                OnProcessStatusChanged?.Invoke("", $"扫码异常 - {ex.Message}");

                await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanResultAddress, 2, "扫码异常");
            }
            finally
            {
                await SafeWritePLCRegister(commManager.GetCurrentConfig().PLC.ScanTriggerAddress, 0, "D500复位");

                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 扫码流程结束 | 条码:{barcode ?? "无"} | 结果:{(scanSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        #region 拧紧轴主动监控

        /// <summary>
        /// 监控拧紧轴直到检测到完成状态
        /// </summary>
        private async Task MonitorTighteningCompletion(string barcode, CancellationToken monitorCts)
        {
            var startTime = DateTime.Now;
            var pollInterval = 500;

            bool hasSeenWorkingState = false;
            int? firstStatusCode = null;
            int consecutiveCompletedCount = 0;

            //扭矩变化检测
            float lastTorque = -1f; // 初始值设为-1，表示未初始化
            bool torqueHasChanged = false;
            DateTime lastTorqueChangeTime = DateTime.MinValue;

            // 日志控制
            DateTime lastWarningTime = DateTime.MinValue;
            DateTime lastUnknownStatusWarning = DateTime.MinValue;

            try
            {
                LogManager.LogInfo($"========== 拧紧监控开始 | 条码:{barcode} | 模式:持续监控（扭矩变化检测）==========");
                OnProcessStatusChanged?.Invoke(barcode, "监控拧紧中...");

                while (GetRunningState() && !monitorCts.IsCancellationRequested)
                {
                    try
                    {
                        var tighteningData = await commManager.ReadTighteningAxisData();

                        if (tighteningData == null)
                        {
                            LogManager.LogWarning("拧紧轴数据读取失败，500ms后重试");
                            await Task.Delay(pollInterval, monitorCts);
                            continue;
                        }

                        int currentStatus = tighteningData.StatusCode;
                        float currentTorque = tighteningData.CompletedTorque;

                        // 记录第一次读取的状态 
                        if (firstStatusCode == null)
                        {
                            firstStatusCode = currentStatus;
                            lastTorque = currentTorque;

                            LogManager.LogInfo($"首次读取拧紧轴状态: {currentStatus} ({tighteningData.GetStatusDisplayName()}) | " +
                                              $"扭矩:{currentTorque:F2}Nm | 条码:{barcode}");

                            if (tighteningData.IsOperationCompleted)
                            {
                                LogManager.LogWarning($"⚠ 首次读取即为完成状态，确认为残留数据，等待状态归零或扭矩变化...");
                            }
                        }

                        // 扭矩变化检测（关键优化点）
                        if (lastTorque >= 0 && Math.Abs(currentTorque - lastTorque) > 0.5f)
                        {
                            if (!torqueHasChanged)
                            {
                                torqueHasChanged = true;
                                lastTorqueChangeTime = DateTime.Now;
                                LogManager.LogInfo($"✓ 检测到扭矩变化 | {lastTorque:F2}Nm → {currentTorque:F2}Nm | " +
                                                  $"条码:{barcode} | 确认设备开始活动");
                            }
                            lastTorque = currentTorque;
                        }
                        else if (lastTorque < 0)
                        {
                            // 首次初始化
                            lastTorque = currentTorque;
                        }

                        // 标记已进入工作状态（扩展判断）
                        if (currentStatus == 0 ||       // 空闲
                            currentStatus == 1 ||       // 运行中
                            currentStatus == 500 ||     // 回零中
                            currentStatus == 1000 ||    // 执行命令中
                            torqueHasChanged)           // 扭矩发生变化
                        {
                            if (!hasSeenWorkingState)
                            {
                                hasSeenWorkingState = true;

                                string trigger = torqueHasChanged ?
                                    $"扭矩变化({lastTorque:F2}Nm)" :
                                    $"状态变化({currentStatus})";

                                LogManager.LogInfo($"✓ 拧紧轴进入工作状态 | 触发条件:{trigger} | 条码:{barcode}");
                            }

                            consecutiveCompletedCount = 0;
                        }

                        // 未知状态检测 
                        var knownStatuses = new[] { 0, 1, 11, 21, 22, 23, 24, 25, 500, 1000 };
                        if (!knownStatuses.Contains(currentStatus))
                        {
                            if ((DateTime.Now - lastUnknownStatusWarning).TotalSeconds >= 60)
                            {
                                LogManager.LogWarning($"⚠ 检测到未知拧紧轴状态码: {currentStatus} | 条码:{barcode} | " +
                                                    $"扭矩:{currentTorque:F2}Nm | 如持续出现请检查设备配置");
                                lastUnknownStatusWarning = DateTime.Now;
                            }
                        }

                        // 运行中UI更新 
                        if (tighteningData.IsRunning)
                        {
                            OnProcessStatusChanged?.Invoke(barcode,
                                $"拧紧中 (实时扭矩:{currentTorque:F2}Nm)");
                        }

                        // 检查完成状态 
                        if (tighteningData.IsOperationCompleted)
                        {
                            consecutiveCompletedCount++;

                            // 必须先经历工作状态
                            if (!hasSeenWorkingState)
                            {
                                bool shouldLogWarning = false;
                                string logReason = "";

                                if (consecutiveCompletedCount == 1)
                                {
                                    // 第1次：立即输出
                                    shouldLogWarning = true;
                                    logReason = "首次检测残留数据";
                                    lastWarningTime = DateTime.Now;
                                }
                                else if (consecutiveCompletedCount == 10)
                                {
                                    // 第10次（5秒后）：确认持续残留
                                    shouldLogWarning = true;
                                    logReason = "持续残留，等待设备活动";
                                    lastWarningTime = DateTime.Now;
                                }
                                else if ((DateTime.Now - lastWarningTime).TotalSeconds >= 60)
                                {
                                    // 之后每60秒输出一次
                                    shouldLogWarning = true;
                                    logReason = "长时间等待提醒";
                                    lastWarningTime = DateTime.Now;
                                }

                                if (shouldLogWarning)
                                {
                                    var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                                    var elapsedMinutes = elapsedSeconds / 60;

                                    LogManager.LogWarning($"⚠ [第{consecutiveCompletedCount}次] 检测到完成状态但未经历工作过程 | " +
                                                        $"状态:{currentStatus} ({tighteningData.GetStatusDisplayName()}) | " +
                                                        $"扭矩:{currentTorque:F2}Nm | " +
                                                        $"条码:{barcode} | " +
                                                        $"原因:{logReason} | " +
                                                        $"已等待:{elapsedMinutes:F1}分钟({elapsedSeconds:F0}秒)");
                                }
                                else
                                {
                                    // 其他时候只输出Debug
                                    LogManager.LogDebug($"[第{consecutiveCompletedCount}次] 仍为残留数据 | 状态:{currentStatus} | 扭矩:{currentTorque:F2}Nm");
                                }

                                await Task.Delay(pollInterval, monitorCts);
                                continue;
                            }

                            // 已经历工作状态，记录完成日志
                            LogManager.LogInfo($"✓ 检测到拧紧完成（有效数据）| " +
                                              $"状态:{currentStatus} ({tighteningData.GetStatusDisplayName()}) | " +
                                              $"扭矩:{currentTorque:F2}Nm | " +
                                              $"角度:{Math.Abs(tighteningData.CompletedAngle):F1}° | " +
                                              $"结果:{tighteningData.QualityResult} | " +
                                              $"条码:{barcode}");

                            // 验证数据有效性
                            if (ValidateTighteningData(tighteningData))
                            {
                                await ProcessCompletedTighteningData(barcode, tighteningData);
                                return; 
                            }
                            else
                            {
                                LogManager.LogWarning("拧紧数据验证失败，继续监控");
                            }
                        }

                        // 长时间监控周期提醒（每2分钟）
                        var elapsed = (DateTime.Now - startTime).TotalSeconds;
                        if (elapsed >= 120 && elapsed % 120 < 0.5)
                        {
                            LogManager.LogInfo($"[持续监控] 条码:{barcode} | 已监控:{elapsed / 60:F1}分钟({elapsed:F0}秒) | " +
                                              $"当前状态:{currentStatus} ({tighteningData.GetStatusDisplayName()}) | " +
                                              $"扭矩:{currentTorque:F2}Nm | " +
                                              $"是否见过工作状态:{hasSeenWorkingState} | " +
                                              $"扭矩是否变化:{torqueHasChanged}");
                        }

                        await Task.Delay(pollInterval, monitorCts);
                    }
                    catch (OperationCanceledException)
                    {
                        LogManager.LogInfo($"拧紧监控被取消（新产品扫码或系统停止）| 条码:{barcode}");
                        return; 
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"拧紧监控异常: {ex.Message}");
                        await Task.Delay(pollInterval, monitorCts);
                    }
                }

                LogManager.LogInfo($"系统已停止，监控任务退出 | 条码:{barcode}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧监控任务异常 | 条码:{barcode} | 错误:{ex.Message}");

                // 只在真正异常时保存降级数据
                if (!monitorCts.IsCancellationRequested && GetRunningState())
                {
                    await SaveFallbackData(barcode, $"监控异常: {ex.Message}");
                }
            }
            finally
            {
                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 拧紧监控结束 | 条码:{barcode} | 耗时:{duration / 60:F1}分钟({duration:F0}秒) ==========");

                ClearBarcodeCache();
                OnCurrentProductChanged?.Invoke("", "等待扫码...");
            }
        }

        /// <summary>
        /// 处理拧紧完成的数据
        /// </summary>
        private async Task ProcessCompletedTighteningData(string barcode, TighteningAxisData tighteningData)
        {
            try
            {
                LogManager.LogInfo($"开始处理拧紧数据 | 条码:{barcode} | 合格:{tighteningData.IsQualified}");

                // 判断是否为NG产品
                if (!tighteningData.IsQualified)
                {
                    LogManager.LogInfo($"检测到UNPASS产品 | 条码:{barcode} | 原因:{tighteningData.QualityResult}");
                    await ProcessProcess4NGProduct(barcode, tighteningData);
                    return;
                }

                // PASS产品处理
                OnProcessStatusChanged?.Invoke(barcode, "拧紧合格 - 保存数据中...");

                // 生成工序14数据
                var tailProcessData = GenerateTailProcessData(barcode, tighteningData);
                var completeData = tailProcessData;

                // 保存到数据库
                await dataManager.SaveProductData(
                    barcode,
                    tailProcessData,
                    completeData,
                    isNG: false,
                    ngProcessId: null
                );

                // 保存到本地文件
                await SaveProductDataToFile(barcode, tailProcessData);

                OnProcessStatusChanged?.Invoke(barcode, "数据已保存 - 上传MES中...");

                // 上传到MES服务器
                bool uploadSuccess = await dataManager.UploadToServer(barcode, completeData);

                if (uploadSuccess)
                {
                    OnProcessStatusChanged?.Invoke(barcode, "上传成功 - 流程完成");
                    LogManager.LogInfo($"数据处理完成 | 条码:{barcode} | 合格:✓ | 上传:✓");
                }
                else
                {
                    OnProcessStatusChanged?.Invoke(barcode, "上传失败 - 已加入重试队列");
                    LogManager.LogWarning($"数据上传失败 | 条码:{barcode} | 已加入重试队列");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理拧紧数据异常 | 条码:{barcode} | 错误:{ex.Message}");
                await SaveFallbackData(barcode, $"数据处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证拧紧数据有效性（简化版）
        /// </summary>
        private bool ValidateTighteningData(TighteningAxisData data)
        {
            if (data == null)
            {
                LogManager.LogWarning("数据验证失败：数据为null");
                return false;
            }

            // 1. 必须已完成（状态码=11或21~30）
            if (!data.IsOperationCompleted)
            {
                LogManager.LogDebug($"数据验证失败：未完成状态（状态码={data.StatusCode}）");
                return false;
            }

            // 2. 必须有完成扭矩
            if (data.CompletedTorque <= 0)
            {
                LogManager.LogDebug("数据验证失败：完成扭矩为0");
                return false;
            }

            // 3. 角度验证（容忍负值，只记录警告）
            var absoluteAngle = Math.Abs(data.CompletedAngle);
            if (absoluteAngle > 5000)  // 假设5000度为异常阈值
            {
                LogManager.LogWarning($"角度数据异常(绝对值:{absoluteAngle:F1}°)，但不阻止流程");
            }

            // 4. 扭矩必须在合理范围内
            if (data.CompletedTorque > 100)
            {
                LogManager.LogWarning($"扭矩数据超出合理范围({data.CompletedTorque:F2}Nm)");
            }

            LogManager.LogDebug($"拧紧数据验证通过 | " +
                              $"扭矩:{data.CompletedTorque:F2}Nm | " +
                              $"角度:{absoluteAngle:F1}° | " +
                              $"状态:{data.GetStatusDisplayName()} | " +
                              $"结果:{data.QualityResult}");

            return true;
        }

        #endregion

        /// <summary>
        /// 安全地写入PLC寄存器（带日志和异常处理）
        /// </summary>
        private async Task SafeWritePLCRegister(string address, int value, string description, int maxRetries = 2)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    if (attempt == 1)
                    {
                        LogManager.LogInfo($"PLC反馈 | 准备写入 {address}={value} ({description})");
                    }
                    else
                    {
                        // 重试前等待更长时间，避免连续失败
                        LogManager.LogInfo($"PLC反馈 | 等待{300}ms后重试...");
                        await Task.Delay(300);

                        LogManager.LogInfo($"PLC反馈 | 重试写入 {address}={value} ({description}) - 第{attempt}次");
                    }

                    bool success = await commManager.WritePLCDRegister(address, value);

                    if (success)
                    {
                        LogManager.LogInfo($"PLC反馈 | {address}={value} ({description}) ✓写入成功");
                        return; // 成功立即返回
                    }
                    else
                    {
                        LogManager.LogWarning($"PLC反馈 | {address}={value} ({description}) ✗写入失败 (尝试{attempt}/{maxRetries})");

                        // 第1次失败后立即重试（不等待）
                        if (attempt == 1)
                        {
                            await Task.Delay(100); // 短暂延迟
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"PLC反馈异常 | {address}={value} ({description}) | " +
                                      $"尝试{attempt}/{maxRetries} | " +
                                      $"错误:{ex.Message}");

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(500); // 异常后等待更长时间
                    }
                }
            }

            LogManager.LogError($"PLC反馈最终失败 | {address}={value} ({description}) | 已重试{maxRetries}次");
        }

        /// <summary>
        /// 保存产品数据到本地文件
        /// </summary>
        private async Task<bool> SaveProductDataToFile(string barcode, string processData)
        {
            try
            {
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductionData");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string passFolder = Path.Combine(baseDir, dateFolder, "PASS");

                if (!Directory.Exists(passFolder))
                {
                    Directory.CreateDirectory(passFolder);
                    LogManager.LogDebug($"创建PASS文件夹: {passFolder}");
                }

                string fileName = $"{SanitizeFileName(barcode)}_PASS_Process14.json";
                string filePath = Path.Combine(passFolder, fileName);

                // 格式化JSON（数组格式）
                var fileContent = processData;

                await Task.Run(() => File.WriteAllText(filePath, fileContent, Encoding.UTF8));
                LogManager.LogDebug($"产品文件已保存: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存产品文件失败: {ex.Message}");
                return false;
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

                // 完整数据仅包含工序4
                var completeData = fallbackData;

                // 保存到数据库（标记为异常）
                await dataManager.SaveProductData(
                    barcode,
                    fallbackData,  // 工序4为异常数据
                    completeData,
                    isNG: true,    // 标记为异常
                    ngProcessId: "13" // 异常发生在工序14
                );

                // 保存到NG文件夹
                await SaveProcess4NGProductToFile(barcode, null, completeData);

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
                ProcessId = "13",
                Code = barcode,
                Data = new[]
                {
                    new
                    {
                        ItemName = "尾椎安装",
                        Remark = "扭矩：无",
                        Result = "FAIL"
                    }
                }
            };

            return JsonConvert.SerializeObject(fallbackProcessData, Formatting.Indented);
        }

        /// <summary>
        /// 处理工序4 NG产品
        /// </summary>
        private async Task ProcessProcess4NGProduct(string productCode, TighteningAxisData tighteningResult)
        {
            try
            {
                var startTime = DateTime.Now;
                LogManager.LogInfo($"========== 开始处理工序4 UNPASS产品 ==========");
                LogManager.LogInfo($"产品码: {productCode}");
                LogManager.LogInfo($"UNPASS原因: {tighteningResult.QualityResult}");
                LogManager.LogInfo($"完成扭矩: {tighteningResult.CompletedTorque:F2}Nm (目标: {tighteningResult.TargetTorque:F2}Nm)");
                LogManager.LogInfo($"完成角度: {Math.Abs(tighteningResult.CompletedAngle):F1}°");

                OnProcessStatusChanged?.Invoke(productCode, $"工序4 UNPASS产品处理中 ({tighteningResult.QualityResult})...");

                // 生成工序14数据
                var tailProcessData = GenerateTailProcessData(productCode, tighteningResult);
                var completeData = tailProcessData;

                // 1. 保存到数据库（标记工序4 NG）
                OnProcessStatusChanged?.Invoke(productCode, "保存UNPASS产品数据 (工序4)...");
                bool dbResult = await dataManager.SaveProductData(
                    productCode,
                    tailProcessData,
                    completeData,
                    isNG: true,
                    ngProcessId: "13"
                );

                if (dbResult)
                {
                    LogManager.LogInfo($"UNPASS产品数据已保存到数据库: {productCode}");
                }
                else
                {
                    LogManager.LogError($"UNPASS产品数据保存数据库失败: {productCode}");
                }

                // 2. 保存到本地NG文件夹
                bool fileResult = await SaveProcess4NGProductToFile(productCode, tighteningResult, completeData);

                // 3. 上传到MES
                OnProcessStatusChanged?.Invoke(productCode, "上传工序4 UNPASS产品数据到MES...");
                bool uploadResult = await dataManager.UploadToServer(productCode, completeData);

                if (uploadResult)
                {
                    LogManager.LogInfo($"UNPASS产品数据已上传到MES: {productCode}");
                    OnProcessStatusChanged?.Invoke(productCode, "工序4 UNPASS产品已上传MES");
                }
                else
                {
                    LogManager.LogWarning($"UNPASS产品数据上传失败，已加入重试队列: {productCode}");
                    OnProcessStatusChanged?.Invoke(productCode, "工序4 UNPASS产品上传失败 - 已加入重试队列");
                }

                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"工序4 UNPASS产品处理完成 | 产品:{productCode} | " +
                                  $"数据库:{dbResult} | 文件:{fileResult} | 上传:{uploadResult} | 耗时:{duration:F1}秒");
                LogManager.LogInfo($"========== 工序4 UNPASS产品处理结束 ==========");

                OnProcessStatusChanged?.Invoke(productCode, $"工序4 UNPASS产品处理完成 ({tighteningResult.QualityResult})");
                await Task.Delay(2000);

                OnCurrentProductChanged?.Invoke("", "等待扫码...");
                OnProcessStatusChanged?.Invoke("", "系统运行中 - 等待扫码");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理工序4 UNPASS产品异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
                OnProcessStatusChanged?.Invoke(productCode, $"工序4 UNPASS产品处理异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 保存工序4 NG产品到本地文件
        /// </summary>
        private async Task<bool> SaveProcess4NGProductToFile(
            string productCode,
            TighteningAxisData tighteningResult,
            string completeData)
        {
            try
            {
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductionData");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string ngFolder = Path.Combine(baseDir, dateFolder, "UNPASS");

                if (!Directory.Exists(ngFolder))
                {
                    Directory.CreateDirectory(ngFolder);
                    LogManager.LogDebug($"创建UNPASS文件夹: {ngFolder}");
                }

                string fileName = $"{SanitizeFileName(productCode)}_UNPASS_Process14.json";
                string filePath = Path.Combine(ngFolder, fileName);

                await Task.Run(() => File.WriteAllText(filePath, completeData, Encoding.UTF8));
                LogManager.LogDebug($"工序4 UNPASS产品文件已保存: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存工序4 UNPASS产品文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return $"UNKNOWN_{DateTime.Now:HHmmss}";

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
                if (!string.IsNullOrEmpty(_currentProductBarcode))
                {
                    LogManager.LogDebug($"清空条码缓存: {_currentProductBarcode}");
                }
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

        #region 扫码相关方法

        private void ProcessBarcodeData(string barcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    LogManager.LogWarning("收到空条码，忽略");
                    return;
                }

                LogManager.LogInfo($"扫描到条码: {barcode}");

                bool taskCompleted = false;

                // 无论如何都先缓存条码（双重保险）
                lock (barcodeLock)
                {
                    // 如果缓存已有值，记录警告（可能是重复扫码）
                    if (!string.IsNullOrEmpty(cachedBarcode) && cachedBarcode != barcode)
                    {
                        LogManager.LogWarning($"覆盖旧的缓存条码: {cachedBarcode} → {barcode}");
                    }
                    cachedBarcode = barcode;
                    LogManager.LogInfo($"条码已缓存: {barcode}");
                }

                // 尝试完成等待任务（如果存在）
                lock (barcodeTaskLock)
                {
                    LogManager.LogInfo($"检查等待任务 | " +
                                     $"barcodeWaitTask={(barcodeWaitTask == null ? "null" : "存在")} | " +
                                     $"IsCompleted={(barcodeWaitTask?.Task.IsCompleted.ToString() ?? "N/A")} | " +
                                     $"Status={(barcodeWaitTask?.Task.Status.ToString() ?? "N/A")}");

                    if (barcodeWaitTask != null)
                    {
                        // 检查任务是否已完成（包括取消、错误等状态）
                        if (barcodeWaitTask.Task.IsCompleted)
                        {
                            LogManager.LogWarning($"等待任务已完成，状态: {barcodeWaitTask.Task.Status}，条码将从缓存读取");
                        }
                        else
                        {
                            // 任务未完成，尝试设置结果
                            try
                            {
                                taskCompleted = barcodeWaitTask.TrySetResult(barcode);

                                if (taskCompleted)
                                {
                                    LogManager.LogInfo($"✓ 等待任务已完成 | 条码:{barcode}");
                                    barcodeWaitTask = null; // 清理
                                }
                                else
                                {
                                    LogManager.LogWarning($"✗ 等待任务设置失败 | 任务状态:{barcodeWaitTask.Task.Status}");
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                LogManager.LogWarning($"任务已处于完成状态，无法设置结果: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogError($"完成等待任务异常: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        LogManager.LogInfo("无等待任务，条码已缓存（将在检查缓存时使用）");
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

        /// <summary>
        /// 持续发送"ON"指令直到扫码成功
        /// </summary>
        private async Task<string> WaitForBarcodeScanWithRetry()
        {
            LogManager.LogInfo("开始持续扫码流程...");

            int retryCount = 0;
            const int maxRetries = 12;
            const int scanInterval = 5000; // 每5秒重试一次

            while (retryCount < maxRetries && GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    retryCount++;

                    lock (barcodeLock)
                    {
                        if (!string.IsNullOrEmpty(cachedBarcode))
                        {
                            LogManager.LogInfo($"发现缓存条码: {cachedBarcode}（在发送指令前）");
                            string result = cachedBarcode;
                            cachedBarcode = null;
                            return result;
                        }
                    }

                    OnProcessStatusChanged?.Invoke("", $"发送扫码指令 (尝试 {retryCount}/{maxRetries})");
                    LogManager.LogInfo($"扫码指令 | 尝试:{retryCount}/{maxRetries}");

                    bool scanCommandSent = await commManager.SendScannerCommand("ON");
                    if (!scanCommandSent)
                    {
                        LogManager.LogWarning($"指令发送失败 | 尝试:{retryCount}/{maxRetries} | {scanInterval / 1000}秒后重试");
                        OnProcessStatusChanged?.Invoke("", $"扫码枪通信失败 - {scanInterval / 1000}秒后重试 ({retryCount}/{maxRetries})");
                        await Task.Delay(scanInterval, cancellationTokenSource.Token);
                        continue;
                    }

                    OnProcessStatusChanged?.Invoke("", $"等待条码扫描... (尝试 {retryCount}/{maxRetries})");

                    try
                    {
                        string barcode = await WaitForBarcodeScanWithFlexibleTimeout(scanInterval, cancellationTokenSource.Token);

                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            LogManager.LogInfo($"扫码成功 | 条码:{barcode} | 尝试次数:{retryCount}");
                            OnProcessStatusChanged?.Invoke("", "条码扫描成功");
                            return barcode;
                        }
                    }
                    catch (TimeoutException)
                    {
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

                        LogManager.LogDebug($"扫码超时 | 尝试:{retryCount}/{maxRetries} | 继续重试");
                        OnProcessStatusChanged?.Invoke("", $"扫码超时，{scanInterval / 1000}秒后重试 (第{retryCount}/{maxRetries}次)");
                    }

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        LogManager.LogInfo("扫码流程被取消");
                        throw new OperationCanceledException("扫码流程被用户取消");
                    }

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
                    await Task.Delay(2000, cancellationTokenSource.Token);
                }
            }

            LogManager.LogError($"扫码重试达到最大次数 ({maxRetries})，放弃扫码");
            OnProcessStatusChanged?.Invoke("", $"扫码失败，已重试{maxRetries}次");
            throw new TimeoutException($"扫码重试达到最大次数 ({maxRetries})");
        }

        private async Task<string> WaitForBarcodeScanWithFlexibleTimeout(int timeoutMs, CancellationToken externalToken)
        {
            // 立即检查缓存
            lock (barcodeLock)
            {
                if (!string.IsNullOrEmpty(cachedBarcode))
                {
                    LogManager.LogInfo($"[检查点1] 使用缓存条码: {cachedBarcode}");
                    string result = cachedBarcode;
                    cachedBarcode = null;
                    return result;
                }
            }

            // 创建等待任务
            TaskCompletionSource<string> waitTask;
            lock (barcodeTaskLock)
            {
                // 清理旧任务（防御性编程）
                if (barcodeWaitTask != null)
                {
                    if (!barcodeWaitTask.Task.IsCompleted)
                    {
                        try
                        {
                            barcodeWaitTask.TrySetCanceled();
                            LogManager.LogWarning("清理了未完成的旧等待任务");
                        }
                        catch { }
                    }
                    else
                    {
                        LogManager.LogDebug($"旧等待任务已完成，状态: {barcodeWaitTask.Task.Status}");
                    }
                }

                barcodeWaitTask = new TaskCompletionSource<string>();
                waitTask = barcodeWaitTask;

                LogManager.LogInfo($"创建新的等待任务 | 超时:{timeoutMs}ms | TaskId:{waitTask.Task.Id}");
            }

            try
            {
                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken))
                {
                    timeoutCts.CancelAfter(timeoutMs);

                    var timeoutTask = Task.Delay(timeoutMs, timeoutCts.Token);
                    var completedTask = await Task.WhenAny(waitTask.Task, timeoutTask);

                    // 等待完成后立即检查
                    if (completedTask == waitTask.Task)
                    {
                        if (!waitTask.Task.IsCanceled && waitTask.Task.IsCompleted)
                        {
                            try
                            {
                                string result = waitTask.Task.Result;
                                LogManager.LogInfo($"[检查点2] 等待任务完成，条码: {result}");
                                return result;
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogError($"获取任务结果异常: {ex.Message}");
                            }
                        }
                        else
                        {
                            LogManager.LogWarning($"等待任务状态异常: IsCanceled={waitTask.Task.IsCanceled}, Status={waitTask.Task.Status}");
                        }
                    }
                    else
                    {
                        LogManager.LogDebug("等待超时，timeoutTask 先完成");
                    }

                    // 超时后第1次检查缓存（200ms缓冲）
                    LogManager.LogDebug("[检查点3] 等待超时，第1次缓冲期检查（200ms）...");
                    await Task.Delay(200, externalToken);

                    lock (barcodeLock)
                    {
                        if (!string.IsNullOrEmpty(cachedBarcode))
                        {
                            LogManager.LogInfo($"[检查点3] 缓冲期发现条码: {cachedBarcode}");
                            string result = cachedBarcode;
                            cachedBarcode = null;
                            return result;
                        }
                    }

                    // 第2次检查缓存（再等300ms）
                    LogManager.LogDebug("[检查点4] 第2次缓冲期检查（300ms）...");
                    await Task.Delay(300, externalToken);

                    lock (barcodeLock)
                    {
                        if (!string.IsNullOrEmpty(cachedBarcode))
                        {
                            LogManager.LogInfo($"[检查点4] 缓冲期发现条码: {cachedBarcode}");
                            string result = cachedBarcode;
                            cachedBarcode = null;
                            return result;
                        }
                    }

                    LogManager.LogDebug($"[检查点4] 所有检查点均未发现条码，确认超时");
                    throw new TimeoutException($"单次扫码超时 ({timeoutMs}ms)");
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("等待任务被取消");

                // 取消时也检查缓存
                lock (barcodeLock)
                {
                    if (!string.IsNullOrEmpty(cachedBarcode))
                    {
                        LogManager.LogInfo($"[检查点5] 取消时发现条码: {cachedBarcode}");
                        string result = cachedBarcode;
                        cachedBarcode = null;
                        return result;
                    }
                }
                throw;
            }
            finally
            {
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask == waitTask)
                    {
                        barcodeWaitTask = null;
                        LogManager.LogDebug($"等待任务已清理 | TaskId:{waitTask.Task.Id}");
                    }
                }
            }
        }

        #endregion

        #region 拧紧轴相关方法

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
                    LogManager.LogDebug($"拧紧操作完成 - 扭矩: {tighteningData.CompletedTorque:F2}Nm, " +
                                      $"角度: {Math.Abs(tighteningData.CompletedAngle):F1}°, " +
                                      $"结果: {tighteningData.QualityResult}");
                }
                else if (tighteningData.IsRunning)
                {
                    // 运行中使用 CompletedTorque（5094在运行中就是实时值）
                    LogManager.LogDebug($"拧紧轴运行中 - 实时扭矩: {tighteningData.CompletedTorque:F2}Nm, " +
                                      $"目标: {tighteningData.TargetTorque:F2}Nm");
                }

                // 用不合格状态替代错误检测
                if (tighteningData.IsOperationCompleted && !tighteningData.IsQualified)
                {
                    LogManager.LogWarning($"拧紧不合格 - 原因: {tighteningData.QualityResult}, " +
                                        $"状态码: {tighteningData.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理拧紧轴数据异常: {ex.Message}");
            }
        }

        #endregion

        #region 数据生成方法

        /// <summary>
        /// 生成工序14数据
        /// </summary>
        private string GenerateTailProcessData(string barcode, TighteningAxisData tighteningResult)
        {
            LogManager.LogDebug($"生成工序13数据: {barcode}");

            // 角度使用绝对值（避免反转导致的负值）
            var absoluteAngle = Math.Abs(tighteningResult.CompletedAngle);

            var processData = new
            {
                ProcessId = "13",
                Code = barcode,
                Data = new[]
                {
                    new
                    {
                        ItemName = "尾椎安装",
                        Remark = $"扭矩：{tighteningResult.CompletedTorque:F2}Nm",
                        Result = tighteningResult.IsQualified ? "PASS" : "FAIL"
                    }
                }
            };

            string jsonResult = JsonConvert.SerializeObject(processData, Formatting.Indented);

            LogManager.LogDebug($"工序13数据生成 | 条码:{barcode} | " +
                               $"扭矩:{tighteningResult.CompletedTorque:F2}Nm | " +
                               $"角度:{absoluteAngle:F1}° | " +
                               $"结果:{(tighteningResult.IsQualified ? "PASS" : "FAIL")}");

            return jsonResult;
        }

        #endregion

        #region 公共方法

        public void EmergencyStop()
        {
            try
            {
                LogManager.LogWarning("执行紧急停止");

                lock (runningStateLock)
                {
                    isRunning = false;
                }

                cancellationTokenSource?.Cancel();

                // 解绑事件
                if (commManager != null)
                {
                    commManager.OnBarcodeScanned -= ProcessBarcodeData;
                    commManager.OnTighteningDataReceived -= ProcessTighteningData;
                }

                // 清理等待任务
                lock (barcodeTaskLock)
                {
                    if (barcodeWaitTask != null && !barcodeWaitTask.Task.IsCompleted)
                    {
                        try
                        {
                            barcodeWaitTask.SetCanceled();
                        }
                        catch (InvalidOperationException) { }
                        barcodeWaitTask = null;
                    }
                }

                lock (barcodeLock)
                {
                    cachedBarcode = null;
                }

                lock (tighteningDataLock)
                {
                    latestTighteningData = null;
                }

                System.Threading.Thread.Sleep(100);
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
                    commManager.OnBarcodeScanned -= ProcessBarcodeData;
                    commManager.OnTighteningDataReceived -= ProcessTighteningData;
                }

                // 更新引用
                commManager = newCommManager;

                // 绑定新事件
                if (commManager != null)
                {
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

        #endregion
    }
}


