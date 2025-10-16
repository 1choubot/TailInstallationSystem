using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// 产品数据缓存类
    /// </summary>
    public class ProductDataBuffer
    {
        public string ProductCode { get; set; }          // 产品码（A壳码）
        public string Process11Data { get; set; }        // 工序11完整JSON
        public string Process12Data { get; set; }        // 工序12完整JSON
        public string Process13Data { get; set; }        // 工序13完整JSON
        public DateTime CreatedTime { get; set; }        // 创建时间
        public DateTime LastUpdateTime { get; set; }     // 最后更新时间
        public bool IsNG { get; set; }                   // 是否NG产品
        public string NGProcessId { get; set; }          // NG发生在哪道工序

        /// <summary>
        /// 数据是否完整（3道工序都收到）
        /// </summary>
        public bool IsComplete =>
            !string.IsNullOrEmpty(Process11Data) &&
            !string.IsNullOrEmpty(Process12Data) &&
            !string.IsNullOrEmpty(Process13Data);

        /// <summary>
        /// 已接收的工序数量
        /// </summary>
        public int ReceivedCount =>
            (string.IsNullOrEmpty(Process11Data) ? 0 : 1) +
            (string.IsNullOrEmpty(Process12Data) ? 0 : 1) +
            (string.IsNullOrEmpty(Process13Data) ? 0 : 1);

        /// <summary>
        /// 获取已收到的工序数据数组
        /// </summary>
        public string[] GetReceivedProcessData()
        {
            var dataList = new List<string>();
            if (!string.IsNullOrEmpty(Process11Data)) dataList.Add(Process11Data);
            if (!string.IsNullOrEmpty(Process12Data)) dataList.Add(Process12Data);
            if (!string.IsNullOrEmpty(Process13Data)) dataList.Add(Process13Data);
            return dataList.ToArray();
        }
    }

    public class TailInstallationController
    {
        private CommunicationManager commManager;
        private DataManager dataManager;
        private CommunicationConfig _config;
        private readonly ConcurrentDictionary<string, ProductDataBuffer> productDataBuffers;
        private const int MAX_PRODUCT_BUFFER_COUNT = 10;
        private const int BUFFER_TIMEOUT_MINUTES = 30;

        // 扫码等待配置
        private const int SCAN_WAIT_TIMEOUT_SECONDS = 3;
        private const int SCAN_WAIT_CHECK_INTERVAL_MS = 500;
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

            // 初始化产品缓存字典
            productDataBuffers = new ConcurrentDictionary<string, ProductDataBuffer>();

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
                // 启动前延迟，确保旧资源完全释放
                await Task.Delay(200);
                LogManager.LogInfo("系统启动前等待资源就绪（200ms）");

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

                // 清空产品数据缓存
                productDataBuffers.Clear();
                LogManager.LogInfo("系统启动，已清空产品数据缓存");

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

                // 6. 等待短暂时间让事件处理完成
                await Task.Delay(200);

                // 7. 释放通讯管理器（会触发资源释放和延迟）
                commManager?.Dispose();

                // 额外等待确保资源完全释放
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
            bool isTighteningProcessing = false;

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

                    // 检查扫码触发
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
                                    // 根据工作模式选择不同的扫码流程
                                    if (GetCurrentWorkMode() == WorkMode.Independent)
                                    {
                                        await ExecuteIndependentModeScan();
                                    }
                                    else
                                    {
                                        await ExecuteFullProcessModeScan();
                                    }
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

                    // 检查拧紧触发
                    if (!isTighteningProcessing)
                    {
                        try
                        {
                            bool tighteningTriggered = await commManager.CheckTighteningTrigger();
                            if (tighteningTriggered)
                            {
                                LogManager.LogInfo("D501触发：开始读取拧紧数据");
                                isTighteningProcessing = true;

                                try
                                {
                                    // 根据工作模式选择不同的拧紧流程
                                    if (GetCurrentWorkMode() == WorkMode.Independent)
                                    {
                                        await ExecuteIndependentModeTightening();
                                    }
                                    else
                                    {
                                        await ExecuteFullProcessModeTightening();
                                    }
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
        /// 独立模式扫码流程 - 仅扫码和缓存条码
        /// </summary>
        private async Task ExecuteIndependentModeScan()
        {
            var startTime = DateTime.Now;
            string barcode = null;
            bool scanSuccess = false;

            try
            {
                LogManager.LogInfo("========== 独立模式扫码流程开始 ==========");
                OnProcessStatusChanged?.Invoke("", "扫码触发（独立模式）");

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

                    // 直接缓存条码，不检查数据完整性
                    lock (_barcodeCacheLock)
                    {
                        _currentProductBarcode = barcode;
                        LogManager.LogInfo($"独立模式：条码已缓存: {barcode}");
                    }

                    OnCurrentProductChanged?.Invoke(barcode, "扫码成功（独立模式）");
                    OnProcessStatusChanged?.Invoke(barcode, "扫码成功 - 等待拧紧触发");

                    LogManager.LogInfo($"独立模式扫码成功 | 条码:{barcode}");

                    // 反馈PLC扫码成功
                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.ScanResultAddress, 1);

                    LogManager.LogDebug("PLC反馈 | D520=1 (扫码OK)");
                }
                else
                {
                    scanSuccess = false;
                    LogManager.LogError("独立模式扫码失败 | 原因:条码为空");

                    // 反馈PLC扫码失败
                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);

                    LogManager.LogDebug("PLC反馈 | D520=2 (扫码NG)");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"独立模式扫码流程异常 | 错误:{ex.Message}");

                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
            }
            finally
            {
                // 复位D500
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanTriggerAddress, 0);
                LogManager.LogDebug("PLC复位 | D500=0");

                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 独立模式扫码流程结束 | 条码:{barcode ?? "无"} | 结果:{(scanSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        /// <summary>
        /// 独立模式拧紧流程 - 仅处理工序14数据
        /// </summary>
        private async Task ExecuteIndependentModeTightening()
        {
            var startTime = DateTime.Now;
            bool dataGetSuccess = false;
            string currentBarcode = null;

            try
            {
                LogManager.LogInfo("========== 独立模式拧紧流程开始 ==========");

                // 获取当前条码
                currentBarcode = GetCurrentBarcode();
                LogManager.LogInfo($"独立模式：条码获取 | {currentBarcode}");

                OnProcessStatusChanged?.Invoke(currentBarcode, "拧紧触发（独立模式）- 读取数据");

                // 检查心跳状态
                if (!commManager.IsHeartbeatRunning())
                {
                    LogManager.LogError("心跳信号未运行，扫码流程可能未正常执行");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "心跳异常，流程中断");

                    // 保存降级数据
                    await SaveIndependentModeFallbackData(currentBarcode, "心跳信号未运行");

                    await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D521=2 (心跳异常)");

                    return;
                }

                LogManager.LogInfo("心跳状态检查通过，继续拧紧流程");

                // 等待拧紧完成
                OnProcessStatusChanged?.Invoke(currentBarcode, "等待拧紧完成（独立模式）...");
                var tighteningResult = await WaitForTighteningCompletion();

                if (tighteningResult != null)
                {
                    dataGetSuccess = true;

                    // 日志记录
                    if (tighteningResult.Success)
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, $"拧紧完成 - 合格 (扭矩:{tighteningResult.Torque:F2}Nm)");
                        LogManager.LogInfo($"独立模式：拧紧合格 | 条码:{currentBarcode} | 扭矩:{tighteningResult.Torque:F2}Nm");
                    }
                    else
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, $"拧紧完成 - 不合格 ({tighteningResult.QualityResult})");
                        LogManager.LogWarning($"独立模式：拧紧不合格 | 条码:{currentBarcode} | 原因:{tighteningResult.QualityResult}");
                    }

                    if (!tighteningResult.Success)
                    {
                        // 独立模式工序4 NG产品特殊处理
                        LogManager.LogInfo($"独立模式：检测到工序4 NG产品，启动专用处理流程: {currentBarcode}");
                        await ProcessProcess4NGProduct(currentBarcode, tighteningResult);

                        // D521 = 1: 数据已获取并处理
                        await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 1);
                        LogManager.LogDebug("PLC反馈 | D521=1 (NG产品数据已处理)");

                        return; // 提前返回，不再执行后续正常流程
                    }

                    OnProcessStatusChanged?.Invoke(currentBarcode, "保存数据中（独立模式）...");

                    // 生成工序14数据
                    var tailProcessData = GenerateTailProcessData(currentBarcode, tighteningResult);

                    // 独立模式：仅包含工序14的数组
                    var completeData = $"[{tailProcessData}]";

                    // 保存到数据库（前3道工序为空）
                    await dataManager.SaveProductData(
                        currentBarcode,
                        new string[0],     // 前3道工序数据为空数组
                        tailProcessData,   // 工序4数据
                        completeData       // 完整数据仅包含工序4
                    );

                    // 保存到本地文件（前3道工序为null）
                    await LocalFileManager.SaveProductionData(
                        currentBarcode,
                        null,             // 工序1为null
                        null,             // 工序2为null
                        null,             // 工序3为null
                        tailProcessData   // 工序4数据
                    );

                    OnProcessStatusChanged?.Invoke(currentBarcode, "数据已保存（独立模式）");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "上传MES数据中（独立模式）...");
                    // 上传到服务器
                    bool uploadSuccess = await dataManager.UploadToServer(currentBarcode, completeData);

                    if (uploadSuccess)
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, "上传成功 - 流程完成（独立模式）");
                        LogManager.LogInfo($"独立模式数据已处理 | 条码:{currentBarcode} | 合格:{tighteningResult.Success} | 数据库:✓ | 文件:✓ | 上传:✓");
                    }
                    else
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, "上传失败 - 已加入重试队列");
                        LogManager.LogInfo($"独立模式数据已处理 | 条码:{currentBarcode} | 合格:{tighteningResult.Success} | 数据库:✓ | 文件:✓ | 上传:✗");
                    }

                    // D521 = 1: 数据获取成功
                    await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 1);
                    LogManager.LogDebug("PLC反馈 | D521=1 (数据获取成功)");
                }
                else
                {
                    dataGetSuccess = false;
                    LogManager.LogError($"独立模式：拧紧数据获取失败 | 条码:{currentBarcode} | 原因:超时或通信异常");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "拧紧数据获取失败（独立模式）");

                    // 保存降级数据
                    await SaveIndependentModeFallbackData(currentBarcode, "拧紧数据获取超时或通信异常");

                    // D521 = 2: 数据获取失败
                    await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D521=2 (数据获取失败)");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"独立模式拧紧流程异常 | 错误:{ex.Message}");

                // 异常也保存降级数据
                await SaveIndependentModeFallbackData(currentBarcode ?? "UNKNOWN", $"流程异常: {ex.Message}");

                await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
            }
            finally
            {
                // 复位D501
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.TighteningTriggerAddress, 0);
                LogManager.LogDebug("PLC复位 | D501=0");

                // 清空条码缓存，准备下一个产品
                ClearBarcodeCache();

                OnCurrentProductChanged?.Invoke("", "等待扫码（独立模式）...");
                OnProcessStatusChanged?.Invoke("", "系统运行中（独立模式）- 等待扫码");
                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 独立模式拧紧流程结束 | 条码:{currentBarcode ?? "UNKNOWN"} | 数据获取:{(dataGetSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        /// <summary>
        /// 保存独立模式降级数据（拧紧数据获取失败时）
        /// </summary>
        private async Task SaveIndependentModeFallbackData(string barcode, string errorReason)
        {
            try
            {
                LogManager.LogWarning($"独立模式：保存降级数据 | 条码:{barcode} | 原因:{errorReason}");

                // 生成降级的工序4数据
                var fallbackData = GenerateFallbackData(barcode, errorReason);

                // 独立模式：完整数据仅包含工序4
                var completeData = $"[{fallbackData}]";

                // 保存到数据库（前3道工序为空）
                await dataManager.SaveProductData(
                    barcode,
                    new string[0],     // 前3道工序为空数组
                    fallbackData,      // 工序4为异常数据
                    completeData
                );

                // 保存到本地文件
                await LocalFileManager.SaveProductionData(
                    barcode,
                    null,             // 工序1为null
                    null,             // 工序2为null
                    null,             // 工序3为null
                    fallbackData      // 工序4异常数据
                );

                LogManager.LogInfo($"独立模式：降级数据保存成功 | 条码:{barcode}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"独立模式：保存降级数据失败 | 条码:{barcode} | 错误:{ex.Message}");
            }
        }

        /// <summary>
        /// 完整流程模式扫码流程 - 检查数据完整性
        /// </summary>
        private async Task ExecuteFullProcessModeScan()
        {
            var startTime = DateTime.Now;
            string barcode = null;
            bool scanSuccess = false;

            try
            {
                LogManager.LogInfo("========== 完整流程模式扫码流程开始 ==========");
                OnProcessStatusChanged?.Invoke("", "扫码触发 - 开始扫码流程");

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
                        LogManager.LogDebug("已清理旧的扫码等待任务");
                    }
                }

                if (!commManager.IsHeartbeatRunning())
                {
                    LogManager.LogWarning("心跳信号未运行，可能影响PLC通讯");
                }

                // 执行扫码
                barcode = await WaitForBarcodeScanWithRetry();

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    scanSuccess = true;

                    OnProcessStatusChanged?.Invoke(barcode, "扫码成功 - 验证数据完整性...");

                    // 数据完整性检查
                    bool dataReady = false;
                    bool isNGProduct = false;
                    string ngProcessId = null;

                    // 先检查是否是NG产品
                    if (productDataBuffers.TryGetValue(barcode, out var buffer))
                    {
                        lock (buffer)
                        {
                            if (buffer.IsNG)
                            {
                                isNGProduct = true;
                                ngProcessId = buffer.NGProcessId;
                            }
                        }

                        if (isNGProduct)
                        {
                            LogManager.LogError($"产品 {barcode} 已标记为NG（工序{ngProcessId}），不应到达此工位");
                            OnCurrentProductChanged?.Invoke(barcode, $"【NG】工序{ngProcessId}不合格");
                            OnProcessStatusChanged?.Invoke(barcode, $"该产品已NG (工序{ngProcessId}) - 请检查");

                            await commManager.WritePLCDRegister(
                                commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
                            LogManager.LogDebug("PLC反馈 | D520=2 (NG产品)");
                            return;
                        }

                        // 等待数据收齐
                        dataReady = await WaitForCompleteData(barcode);
                    }
                    else
                    {
                        LogManager.LogWarning($"产品 {barcode} 不在缓存中，可能是新产品或数据未到达");

                        OnProcessStatusChanged?.Invoke(barcode, "未找到产品数据 - 等待接收...");
                        // 给一个机会等待数据
                        await Task.Delay(1000);
                        dataReady = await WaitForCompleteData(barcode);
                    }

                    if (!dataReady)
                    {
                        LogManager.LogError($"产品 {barcode} 数据未收齐，无法继续");
                        OnCurrentProductChanged?.Invoke(barcode, "数据未收齐");
                        OnProcessStatusChanged?.Invoke(barcode, "前3道工序数据未收齐 - 无法继续");

                        await commManager.WritePLCDRegister(
                            commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
                        LogManager.LogDebug("PLC反馈 | D520=2 (数据未收齐)");
                        return;
                    }

                    OnProcessStatusChanged?.Invoke(barcode, "数据验证通过 (3/3) - 等待拧紧触发");

                    // 数据就绪，缓存条码
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
                LogManager.LogError($"完整流程模式扫码流程异常 | 错误:{ex.Message}");

                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanResultAddress, 2);
            }
            finally
            {
                await commManager.WritePLCDRegister(
                    commManager.GetCurrentConfig().PLC.ScanTriggerAddress, 0);
                LogManager.LogDebug("PLC复位 | D500=0");

                var duration = (DateTime.Now - startTime).TotalSeconds;

                LogManager.LogInfo($"========== 完整流程模式扫码流程结束 | 条码:{barcode ?? "无"} | 结果:{(scanSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        /// <summary>
        /// 完整流程模式拧紧流程 - 合并4道工序数据
        /// </summary>
        private async Task ExecuteFullProcessModeTightening()
        {
            var startTime = DateTime.Now;
            bool dataGetSuccess = false;
            string currentBarcode = null;

            try
            {
                LogManager.LogInfo("========== 完整流程模式拧紧流程开始 ==========");

                currentBarcode = GetCurrentBarcode();
                LogManager.LogInfo($"条码获取 | {currentBarcode}");

                OnProcessStatusChanged?.Invoke(currentBarcode, "拧紧触发 - 准备读取数据");
                if (!commManager.IsHeartbeatRunning())
                {
                    LogManager.LogError("心跳信号未运行，扫码流程可能未正常执行");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "心跳异常，流程中断");

                    // 保存降级数据
                    await SaveFallbackData(currentBarcode, "心跳信号未运行");

                    await commManager.WritePLCDRegister(
                        commManager.GetCurrentConfig().PLC.TighteningResultAddress, 2);
                    LogManager.LogDebug("PLC反馈 | D521=2 (心跳异常)");

                    return;
                }

                LogManager.LogInfo("心跳状态检查通过，继续拧紧流程");

                OnProcessStatusChanged?.Invoke(currentBarcode, "等待拧紧完成...");
                var tighteningResult = await WaitForTighteningCompletion();

                if (tighteningResult != null)
                {
                    dataGetSuccess = true;

                    // 日志记录
                    if (tighteningResult.Success)
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, $"拧紧完成 - 合格 (扭矩:{tighteningResult.Torque:F2}Nm)");
                        LogManager.LogInfo($"拧紧合格 | 条码:{currentBarcode} | 扭矩:{tighteningResult.Torque:F2}Nm");
                    }
                    else
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, $"拧紧完成 - 不合格 ({tighteningResult.QualityResult})");
                        LogManager.LogWarning($"拧紧不合格 | 条码:{currentBarcode} | 原因:{tighteningResult.QualityResult}");
                    }

                    if (!tighteningResult.Success)
                    {
                        // 工序4 NG产品特殊处理
                        LogManager.LogInfo($"检测到工序4 NG产品，启动专用处理流程: {currentBarcode}");
                        await ProcessProcess4NGProduct(currentBarcode, tighteningResult);

                        // D521 = 1: 数据已获取并处理
                        await commManager.WritePLCDRegister(
                            commManager.GetCurrentConfig().PLC.TighteningResultAddress, 1);
                        LogManager.LogDebug("PLC反馈 | D521=1 (NG产品数据已处理)");

                        return; // 提前返回，不再执行后续正常流程
                    }

                    OnProcessStatusChanged?.Invoke(currentBarcode, "保存数据中...");

                    // 原有逻辑：仅对合格品执行
                    var processDataArray = SafeGetProcessData();
                    var tailProcessData = GenerateTailProcessData(currentBarcode, tighteningResult);
                    var completeData = CombineAllProcessData(tailProcessData, processDataArray);

                    // 保存到数据库
                    await dataManager.SaveProductData(
                        currentBarcode, processDataArray, tailProcessData, completeData);

                    // 保存本地文件（正常目录）
                    await LocalFileManager.SaveProductionData(
                        currentBarcode,
                        processDataArray.Length > 0 ? processDataArray[0] : null,
                        processDataArray.Length > 1 ? processDataArray[1] : null,
                        processDataArray.Length > 2 ? processDataArray[2] : null,
                        tailProcessData
                    );

                    OnProcessStatusChanged?.Invoke(currentBarcode, "数据已保存 (数据库✓ 文件✓)");
                    OnProcessStatusChanged?.Invoke(currentBarcode, "上传MES数据中...");

                    bool uploadSuccess = await dataManager.UploadToServer(currentBarcode, completeData);

                    if (uploadSuccess)
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, "上传成功 - 流程完成");
                        LogManager.LogInfo($"数据已处理 | 条码:{currentBarcode} | 合格:{tighteningResult.Success} | 数据库:✓ | 文件:✓ | 上传:✓");
                    }
                    else
                    {
                        OnProcessStatusChanged?.Invoke(currentBarcode, "上传失败 - 已加入重试队列");
                        LogManager.LogInfo($"数据已处理 | 条码:{currentBarcode} | 合格:{tighteningResult.Success} | 数据库:✓ | 文件:✓ | 上传:✗");
                    }

                    // D521 = 1: 数据获取成功
                    await commManager.WritePLCDRegister(commManager.GetCurrentConfig().PLC.TighteningResultAddress, 1);
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
                LogManager.LogError($"完整流程模式拧紧流程异常 | 错误:{ex.Message}");
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

                OnCurrentProductChanged?.Invoke("", "等待产品数据...");
                OnProcessStatusChanged?.Invoke("", "系统运行中 - 等待产品数据");
                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"========== 完整流程模式拧紧流程结束 | 条码:{currentBarcode ?? "UNKNOWN"} | 数据获取:{(dataGetSuccess ? "成功" : "失败")} | 耗时:{duration:F1}秒 ==========");
            }
        }

        /// <summary>
        /// 清空产品数据缓存（工作模式切换时调用）
        /// </summary>
        public void ClearProductDataBuffers()
        {
            try
            {
                int oldCount = productDataBuffers.Count;

                if (oldCount > 0)
                {
                    // 记录被清空的产品列表
                    var productCodes = productDataBuffers.Keys.ToList();

                    productDataBuffers.Clear();

                    LogManager.LogWarning($"已清空 {oldCount} 个产品缓存，产品码: {string.Join(", ", productCodes)}");
                }
                else
                {
                    LogManager.LogDebug("产品缓存为空，无需清空");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"清空产品缓存异常: {ex.Message}");
                throw;
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

        //接收PC传输数据
        private void ProcessReceivedData(string jsonData)
        {
            // 独立模式判断
            if (GetCurrentWorkMode() == WorkMode.Independent)
            {
                LogManager.LogInfo($"独立模式：已忽略前端发送的工序数据（大小:{jsonData?.Length ?? 0}字节）");
                return;
            }

            try
            {
                // 提取产品码和工序ID
                string productCode = ExtractProductCode(jsonData);
                if (string.IsNullOrEmpty(productCode))
                {
                    LogManager.LogError("无法从工序数据中提取产品码，数据被丢弃");
                    LogManager.LogError($"原始数据: {jsonData?.Substring(0, Math.Min(200, jsonData?.Length ?? 0))}");
                    return;
                }

                string processId = ExtractProcessId(jsonData);
                if (string.IsNullOrEmpty(processId) || !IsValidProcessId(processId))
                {
                    LogManager.LogWarning($"工序ID异常: {processId}，数据被丢弃");
                    return;
                }

                // 获取或创建产品缓存
                var buffer = productDataBuffers.GetOrAdd(productCode, key => new ProductDataBuffer
                {
                    ProductCode = key,
                    CreatedTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now,
                    IsNG = false
                });

                // 检查是否重复数据
                lock (buffer)
                {
                    bool isDuplicate = false;

                    switch (processId)
                    {
                        case "11":
                            if (!string.IsNullOrEmpty(buffer.Process11Data))
                            {
                                isDuplicate = true;
                                LogManager.LogWarning($"产品 {productCode} | 工序11数据重复，忽略");
                            }
                            else
                            {
                                buffer.Process11Data = jsonData;
                                LogManager.LogInfo($"产品 {productCode} | 工序11数据已接收 | 进度:{buffer.ReceivedCount}/3");
                                OnCurrentProductChanged?.Invoke(productCode, $"工序1数据已接收 ({buffer.ReceivedCount}/3)");
                                OnProcessStatusChanged?.Invoke(productCode, $"接收工序数据... ({buffer.ReceivedCount}/3)");
                            }
                            break;

                        case "12":
                            if (!string.IsNullOrEmpty(buffer.Process12Data))
                            {
                                isDuplicate = true;
                                LogManager.LogWarning($"产品 {productCode} | 工序12数据重复，忽略");
                            }
                            else
                            {
                                buffer.Process12Data = jsonData;
                                LogManager.LogInfo($"产品 {productCode} | 工序12数据已接收 | 进度:{buffer.ReceivedCount}/3");
                                OnCurrentProductChanged?.Invoke(productCode, $"工序2数据已接收 ({buffer.ReceivedCount}/3)");
                                OnProcessStatusChanged?.Invoke(productCode, $"接收工序数据... ({buffer.ReceivedCount}/3)");
                            }
                            break;

                        case "13":
                            if (!string.IsNullOrEmpty(buffer.Process13Data))
                            {
                                isDuplicate = true;
                                LogManager.LogWarning($"产品 {productCode} | 工序13数据重复，忽略");
                            }
                            else
                            {
                                buffer.Process13Data = jsonData;
                                LogManager.LogInfo($"产品 {productCode} | 工序13数据已接收 | 进度:{buffer.ReceivedCount}/3");
                                OnCurrentProductChanged?.Invoke(productCode, $"工序3数据已接收 ({buffer.ReceivedCount}/3)");
                                OnProcessStatusChanged?.Invoke(productCode, $"接收工序数据... ({buffer.ReceivedCount}/3)");
                            }
                            break;
                    }

                    buffer.LastUpdateTime = DateTime.Now;

                    // 返回OK响应（TODO: 实际需要通过TCP返回）
                     SendOKResponse(productCode, processId, buffer.ReceivedCount, isDuplicate);

                    // 检测是否包含NG
                    if (!isDuplicate && !buffer.IsNG)
                    {
                        bool hasNG = CheckForNGResult(jsonData);
                        if (hasNG)
                        {
                            LogManager.LogWarning($"产品 {productCode} | 工序{processId}检测到NG");
                            buffer.IsNG = true;
                            buffer.NGProcessId = processId;

                            OnCurrentProductChanged?.Invoke(productCode, $"【NG】工序{processId}不合格");
                            OnProcessStatusChanged?.Invoke(productCode, $"检测到NG产品 (工序{processId}) - 准备处理");

                            // 立即处理NG产品
                            _ = Task.Run(() => ProcessNGProduct(productCode));
                        }
                        else
                        {
                            // 检查数据是否收齐
                            if (buffer.IsComplete)
                            {
                                LogManager.LogInfo($"产品 {productCode} | 前3道工序数据已收齐，等待扫码触发");
                                OnCurrentProductChanged?.Invoke(productCode, "数据已收齐 (3/3) - 等待扫码");
                                OnProcessStatusChanged?.Invoke(productCode, "前3道工序数据已收齐 - 等待扫码触发");
                            }
                        }
                    }
                }

                // 清理过期缓存
                CleanupExpiredBuffers();

                // 检查缓存数量
                if (productDataBuffers.Count > MAX_PRODUCT_BUFFER_COUNT)
                {
                    LogManager.LogWarning($"产品缓存数量超限: {productDataBuffers.Count}/{MAX_PRODUCT_BUFFER_COUNT}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理工序数据异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
            }
        }

        private async Task SendOKResponse(string productCode, string processId, int receivedCount, bool isDuplicate)
        {
            try
            {
                var response = new
                {
                    Status = "OK",
                    ProductCode = productCode,
                    ProcessId = processId,
                    ReceivedCount = receivedCount,
                    IsDuplicate = isDuplicate,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 需要通过CommunicationManager发送响应
                await commManager.SendResponseToClient("", response);

                LogManager.LogDebug($"已发送OK响应 | 产品:{productCode} | 工序:{processId} | 重复:{isDuplicate}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"发送OK响应失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从JSON提取产品码
        /// </summary>
        private string ExtractProductCode(string jsonData)
        {
            try
            {
                var json = JObject.Parse(jsonData);

                // 尝试提取ProcessId判断工序
                string processId = json["ProcessId"]?.ToString()
                                ?? json["processId"]?.ToString();

                string code = null;

                if (processId == "11")
                {
                    // 工序11：提取CodeA字段
                    code = json["CodeA"]?.ToString();
                }
                else if (processId == "12" || processId == "13" || processId == "14")
                {
                    // 工序12/13/14：提取Code/code字段
                    code = json["Code"]?.ToString()
                        ?? json["code"]?.ToString();
                }

                return code?.Trim();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"解析产品码失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从JSON提取工序ID
        /// </summary>
        private string ExtractProcessId(string jsonData)
        {
            try
            {
                var json = JObject.Parse(jsonData);

                string processId = json["ProcessId"]?.ToString()
                                ?? json["processId"]?.ToString();

                return processId?.Trim();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"解析工序ID失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证工序ID是否有效
        /// </summary>
        private bool IsValidProcessId(string processId)
        {
            return processId == "11" || processId == "12" || processId == "13";
        }

        /// <summary>
        /// 检查JSON数据中是否包含NG结果
        /// </summary>
        private bool CheckForNGResult(string jsonData)
        {
            try
            {
                var json = JObject.Parse(jsonData);

                // 获取Data数组（兼容大小写）
                var dataArray = json["Data"] as JArray ?? json["data"] as JArray;

                if (dataArray == null) return false;

                foreach (var item in dataArray)
                {
                    string result = item["Result"]?.ToString()?.ToUpper()
                                 ?? item["result"]?.ToString()?.ToUpper();

                    if (result == "NG")
                    {
                        string itemName = item["ItemName"]?.ToString() ?? "未知项目";
                        LogManager.LogWarning($"检测到NG项目: {itemName}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"检查NG结果失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理过期的产品缓存
        /// </summary>
        private void CleanupExpiredBuffers()
        {
            try
            {
                var expiredTime = DateTime.Now.AddMinutes(-BUFFER_TIMEOUT_MINUTES);

                var expiredProducts = productDataBuffers
                    .Where(kvp => kvp.Value.LastUpdateTime < expiredTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var productCode in expiredProducts)
                {
                    if (productDataBuffers.TryRemove(productCode, out var buffer))
                    {
                        LogManager.LogWarning($"产品 {productCode} 缓存超时被清理 | 进度:{buffer.ReceivedCount}/3 | NG:{buffer.IsNG}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"清理过期缓存异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理NG产品
        /// </summary>
        private async Task ProcessNGProduct(string productCode)
        {
            try
            {
                if (!productDataBuffers.TryGetValue(productCode, out var buffer))
                {
                    LogManager.LogError($"NG产品处理失败：未找到产品 {productCode} 的缓存");
                    return;
                }

                // 在lock外部声明需要的变量
                string[] processDataArray;
                string completeData;
                WorkMode originalMode;
                string ngProcessId;
                int receivedCount;
                DateTime createdTime;  // 添加这个

                // 在lock内部获取数据
                lock (buffer)
                {
                    LogManager.LogInfo($"========== 开始处理NG产品 ==========");
                    LogManager.LogInfo($"产品码: {productCode}");
                    LogManager.LogInfo($"NG工序: {buffer.NGProcessId}");
                    LogManager.LogInfo($"已收到工序数: {buffer.ReceivedCount}");

                    // 获取已收到的所有工序数据
                    processDataArray = buffer.GetReceivedProcessData();

                    // 生成合并数据（JSON数组格式）
                    completeData = "[" + string.Join(",", processDataArray) + "]";

                    // 保存必要信息
                    ngProcessId = buffer.NGProcessId;
                    receivedCount = buffer.ReceivedCount;
                    createdTime = buffer.CreatedTime;  
                }

                OnProcessStatusChanged?.Invoke(productCode, $"NG产品处理中 (工序{ngProcessId})...");
                // 重新构建一个简化的buffer对象用于文件保存
                var bufferForFile = new ProductDataBuffer
                {
                    ProductCode = productCode,
                    NGProcessId = ngProcessId,
                    CreatedTime = createdTime,
                    IsNG = true,
                    // 根据实际收到的数据填充对应字段，ReceivedCount 会自动计算
                    Process11Data = processDataArray.Length > 0 ? processDataArray[0] : null,
                    Process12Data = processDataArray.Length > 1 ? processDataArray[1] : null,
                    Process13Data = processDataArray.Length > 2 ? processDataArray[2] : null
                };

                // 在lock外部执行异步操作
                originalMode = GetCurrentWorkMode();
                try
                {
                    OnProcessStatusChanged?.Invoke(productCode, $"保存NG产品数据 (包含{receivedCount}道工序)...");
                    // 保存到数据库
                    bool saveResult = await dataManager.SaveProductData(
                        productCode,
                        processDataArray,
                        null,  // 没有工序14数据
                        completeData
                    );

                    if (saveResult)
                    {
                        LogManager.LogInfo($"NG产品数据已保存到数据库: {productCode}");
                        OnProcessStatusChanged?.Invoke(productCode, "NG产品数据已保存到数据库");
                    }
                    else
                    {
                        LogManager.LogError($"NG产品数据保存失败: {productCode}");
                    }

                    // 保存到本地文件（NG文件夹）
                    bool fileResult = await SaveNGProductToFile(productCode, bufferForFile, completeData);
                    OnProcessStatusChanged?.Invoke(productCode, $"上传NG产品数据到MES...");

                    // 上传到MES
                    bool uploadResult = await dataManager.UploadToServer(productCode, completeData);
                    if (uploadResult)
                    {
                        LogManager.LogInfo($"NG产品数据已上传到MES: {productCode}");
                        OnProcessStatusChanged?.Invoke(productCode,$"NG产品已上传MES (包含{receivedCount}道工序数据)");
                    }
                    else
                    {
                        LogManager.LogWarning($"NG产品数据上传失败，已加入重试队列: {productCode}");
                        OnProcessStatusChanged?.Invoke(productCode, "NG产品上传失败 - 已加入重试队列");
                    }

                    LogManager.LogInfo($"NG产品处理完成 | 产品:{productCode} | 数据库:{saveResult} | 文件:{fileResult} | 上传:{uploadResult}");
                }
                finally
                {
                    // 恢复原始工作模式
                    UpdateWorkMode(originalMode);
                }

                // 处理完成后从缓存中移除
                if (productDataBuffers.TryRemove(productCode, out _))
                {
                    LogManager.LogInfo($"NG产品 {productCode} 已从缓存移除");
                }

                OnProcessStatusChanged?.Invoke(productCode, $"NG产品处理完成 (工序{ngProcessId})");
                await Task.Delay(2000); // 给用户2秒时间看到完成状态
                OnCurrentProductChanged?.Invoke("", "等待产品数据...");
                OnProcessStatusChanged?.Invoke("", "系统运行中 - 等待产品数据");
                LogManager.LogInfo($"========== NG产品处理结束 ==========");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理NG产品异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");

                OnProcessStatusChanged?.Invoke(productCode, $"NG产品处理异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 保存NG产品到本地文件
        /// </summary>
        private async Task<bool> SaveNGProductToFile(string productCode, ProductDataBuffer buffer, string completeData)
        {
            try
            {
                // 构建文件路径
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductionData");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string ngFolder = Path.Combine(baseDir, dateFolder, "NG");
                // 确保目录存在
                if (!Directory.Exists(ngFolder))
                {
                    Directory.CreateDirectory(ngFolder);
                    LogManager.LogInfo($"创建NG文件夹: {ngFolder}");
                }
                // 生成文件名
                string fileName = $"{SanitizeFileName(productCode)}_NG_Process{buffer.NGProcessId}.json";
                string filePath = Path.Combine(ngFolder, fileName);

                // 异步保存文件
                await Task.Run(() => File.WriteAllText(filePath, completeData, Encoding.UTF8));
                LogManager.LogInfo($"NG产品文件已保存: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存NG产品文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理工序4（拧紧）NG产品
        /// </summary>
        private async Task ProcessProcess4NGProduct(string productCode, TighteningResult tighteningResult)
        {
            try
            {
                var startTime = DateTime.Now;
                LogManager.LogInfo($"========== 开始处理工序4 NG产品 ==========");
                LogManager.LogInfo($"产品码: {productCode}");
                LogManager.LogInfo($"NG原因: {tighteningResult.QualityResult}");
                LogManager.LogInfo($"完成扭矩: {tighteningResult.Torque:F2}Nm (目标: {tighteningResult.TargetTorque:F2}Nm)");

                OnProcessStatusChanged?.Invoke(productCode, $"工序4 NG产品处理中 ({tighteningResult.QualityResult})...");

                // 根据工作模式获取前3道工序数据
                string[] processDataArray;
                string completeData;

                if (GetCurrentWorkMode() == Models.WorkMode.Independent)
                {
                    // 独立模式：仅包含工序4
                    processDataArray = new string[0];
                    var tailProcessData = GenerateTailProcessData(productCode, tighteningResult);
                    completeData = $"[{tailProcessData}]";

                    LogManager.LogInfo("独立模式：仅保存工序4数据");
                }
                else
                {
                    // 完整流程模式：包含前3道工序
                    processDataArray = SafeGetProcessData();
                    var tailProcessData = GenerateTailProcessData(productCode, tighteningResult);
                    completeData = CombineAllProcessData(tailProcessData, processDataArray);

                    int receivedCount = processDataArray.Count(p => !string.IsNullOrEmpty(p));
                    LogManager.LogInfo($"完整流程模式：包含{receivedCount}道前序工序数据");
                }

                // 1. 保存到数据库
                OnProcessStatusChanged?.Invoke(productCode, "保存NG产品数据 (工序4)...");
                bool dbResult = await dataManager.SaveProductData(
                    productCode,
                    processDataArray,
                    null,  // 工序4数据已包含在completeData中
                    completeData
                );

                if (dbResult)
                {
                    LogManager.LogInfo($"NG产品数据已保存到数据库: {productCode}");
                }
                else
                {
                    LogManager.LogError($"NG产品数据保存数据库失败: {productCode}");
                }

                // 2. 保存到本地NG文件夹
                bool fileResult = await SaveProcess4NGProductToFile(productCode, tighteningResult, completeData);

                // 3. 上传到MES
                OnProcessStatusChanged?.Invoke(productCode, "上传工序4 NG产品数据到MES...");
                bool uploadResult = await dataManager.UploadToServer(productCode, completeData);

                if (uploadResult)
                {
                    LogManager.LogInfo($"NG产品数据已上传到MES: {productCode}");
                    OnProcessStatusChanged?.Invoke(productCode, "工序4 NG产品已上传MES");
                }
                else
                {
                    LogManager.LogWarning($"NG产品数据上传失败，已加入重试队列: {productCode}");
                    OnProcessStatusChanged?.Invoke(productCode, "工序4 NG产品上传失败 - 已加入重试队列");
                }

                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogManager.LogInfo($"工序4 NG产品处理完成 | 产品:{productCode} | 数据库:{dbResult} | 文件:{fileResult} | 上传:{uploadResult} | 耗时:{duration:F1}秒");
                LogManager.LogInfo($"========== 工序4 NG产品处理结束 ==========");

                OnProcessStatusChanged?.Invoke(productCode, $"工序4 NG产品处理完成 ({tighteningResult.QualityResult})");
                await Task.Delay(2000); // 给用户2秒时间看到完成状态

                OnCurrentProductChanged?.Invoke("", "等待产品数据...");
                OnProcessStatusChanged?.Invoke("", "系统运行中 - 等待产品数据");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理工序4 NG产品异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
                OnProcessStatusChanged?.Invoke(productCode, $"工序4 NG产品处理异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 保存工序4 NG产品到本地文件
        /// </summary>
        private async Task<bool> SaveProcess4NGProductToFile(string productCode, TighteningResult tighteningResult, string completeData)
        {
            try
            {
                // 构建文件路径
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductionData");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string ngFolder = Path.Combine(baseDir, dateFolder, "NG");

                // 确保目录存在
                if (!Directory.Exists(ngFolder))
                {
                    Directory.CreateDirectory(ngFolder);
                    LogManager.LogInfo($"创建NG文件夹: {ngFolder}");
                }

                // 生成文件名
                string fileName = $"{SanitizeFileName(productCode)}_NG_Process14.json";
                string filePath = Path.Combine(ngFolder, fileName);

                // 异步保存文件
                await Task.Run(() => File.WriteAllText(filePath, completeData, Encoding.UTF8));
                LogManager.LogInfo($"工序4 NG产品文件已保存: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存工序4 NG产品文件失败: {ex.Message}");
                return false;
            }
        }


        /// 
        /// 清理文件名中的非法字符
        /// 
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
            // 独立模式判断（第一道防线）
            if (GetCurrentWorkMode() == WorkMode.Independent)
            {
                LogManager.LogInfo("独立模式：跳过前3道工序数据获取");
                return new string[0];  // 返回空数组
            }

            string currentBarcode = GetCurrentBarcode();

            if (string.IsNullOrEmpty(currentBarcode))
            {
                LogManager.LogError("未找到当前产品条码，无法获取工序数据");
                return new string[3];
            }

            // 防御性检查，确认是完整流程模式
            if (GetCurrentWorkMode() != WorkMode.FullProcess)
            {
                LogManager.LogWarning($"非完整流程模式调用SafeGetProcessData，当前模式: {GetCurrentWorkMode()}");
                return new string[0];
            }

            // 根据条码查找对应产品的数据
            if (productDataBuffers.TryGetValue(currentBarcode, out var buffer))
            {
                lock (buffer)
                {
                    if (!buffer.IsComplete)
                    {
                        LogManager.LogWarning($"产品 {currentBarcode} 数据不完整 | 进度:{buffer.ReceivedCount}/3");
                    }

                    var processDataArray = new string[3];
                    processDataArray[0] = buffer.Process11Data;
                    processDataArray[1] = buffer.Process12Data;
                    processDataArray[2] = buffer.Process13Data;

                    // 数据使用后移除缓存
                    productDataBuffers.TryRemove(currentBarcode, out _);
                    LogManager.LogInfo($"产品 {currentBarcode} 数据已提取并移除缓存");

                    return processDataArray;
                }
            }
            else
            {
                LogManager.LogWarning($"未找到产品 {currentBarcode} 的缓存数据");
                return new string[3];
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
                    OnProcessStatusChanged?.Invoke("", $"发送扫码指令 (尝试 {retryCount}/{maxRetries})");
                    LogManager.LogInfo($"扫码指令 | 尝试:{retryCount}/{maxRetries}");

                    bool scanCommandSent = await commManager.SendScannerCommand("ON");
                    if (!scanCommandSent)
                    {
                        LogManager.LogWarning($"指令发送失败 | 尝试:{retryCount}/{maxRetries} | {scanInterval / 1000}秒后重试");
                        OnProcessStatusChanged?.Invoke("", $"扫码枪通信失败 - {scanInterval / 1000}秒后重试 ({retryCount}/{maxRetries})");

                        // 等待后继续重试
                        await Task.Delay(scanInterval, cancellationTokenSource.Token);
                        continue;
                    }

                    // 步骤2：等待条码扫描（较短超时）
                    OnProcessStatusChanged?.Invoke("", $"等待条码扫描... (尝试 {retryCount}/{maxRetries})");

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

        /// <summary>
        /// 等待数据收齐（处理网络延迟）
        /// </summary>
        private async Task<bool> WaitForCompleteData(string productCode)
        {
            var startTime = DateTime.Now;
            var maxWaitTime = TimeSpan.FromSeconds(SCAN_WAIT_TIMEOUT_SECONDS);
            int checkCount = 0;

            LogManager.LogInfo($"检查产品 {productCode} 数据完整性...");

            OnProcessStatusChanged?.Invoke(productCode, "验证数据完整性...");
            while (DateTime.Now - startTime < maxWaitTime)
            {
                checkCount++;

                if (productDataBuffers.TryGetValue(productCode, out var buffer))
                {
                    lock (buffer)
                    {
                        if (buffer.IsComplete)
                        {
                            LogManager.LogInfo($"产品 {productCode} 数据完整 | 检查次数:{checkCount}");
                            OnProcessStatusChanged?.Invoke(productCode, "数据验证通过 (3/3)");
                            return true;
                        }
                        else
                        {
                            if (checkCount % 2 == 0) // 每2次检查更新一次，避免闪烁
                            {
                                OnProcessStatusChanged?.Invoke(productCode,
                                    $"等待数据收齐... ({buffer.ReceivedCount}/3)");
                            }
                            LogManager.LogDebug($"产品 {productCode} 数据未完整 | 进度:{buffer.ReceivedCount}/3 | 等待中...");
                        }
                    }
                }
                else
                {
                    LogManager.LogWarning($"产品 {productCode} 不在缓存中");
                    OnProcessStatusChanged?.Invoke(productCode, "未找到产品缓存数据");
                    return false;
                }

                await Task.Delay(SCAN_WAIT_CHECK_INTERVAL_MS);
            }

            // 超时后最终检查
            if (productDataBuffers.TryGetValue(productCode, out var finalBuffer))
            {
                lock (finalBuffer)
                {
                    if (finalBuffer.IsComplete)
                    {
                        LogManager.LogInfo($"产品 {productCode} 数据在超时前收齐");
                        OnProcessStatusChanged?.Invoke(productCode, "数据验证通过 (3/3)");
                        return true;
                    }
                    else
                    {
                        LogManager.LogError($"产品 {productCode} 数据等待超时 | 最终进度:{finalBuffer.ReceivedCount}/3");
                        // 记录缺失的工序
                        var missing = new List<string>();
                        if (string.IsNullOrEmpty(finalBuffer.Process11Data)) missing.Add("工序11");
                        if (string.IsNullOrEmpty(finalBuffer.Process12Data)) missing.Add("工序12");
                        if (string.IsNullOrEmpty(finalBuffer.Process13Data)) missing.Add("工序13");
                        LogManager.LogError($"缺失的工序: {string.Join(", ", missing)}");
                        OnProcessStatusChanged?.Invoke(productCode,$"数据等待超时 ({finalBuffer.ReceivedCount}/3) - 缺失: {string.Join(", ", missing)}");
                        return false;
                    }
                }
            }
            OnProcessStatusChanged?.Invoke(productCode, "数据验证失败 - 超时");

            return false;
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
            var currentMode = GetCurrentWorkMode();

            // 独立模式判断
            if (currentMode == WorkMode.Independent)
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

            // 完整流程模式
            LogManager.LogInfo($"完整流程模式：合并4道工序数据");

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

            LogManager.LogInfo($"完整流程数据合并完成 | " +
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

                // 5. 等待短暂时间让事件处理完成
                System.Threading.Thread.Sleep(100);

                // 6. 最后释放通讯管理器
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
