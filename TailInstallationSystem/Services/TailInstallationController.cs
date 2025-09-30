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
            bool isProcessing = false; // 添加处理标志，防止重复处理

            while (GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    bool shouldOutputDebug = (now - lastDebugOutput).TotalSeconds >= 10;

                    if (shouldOutputDebug)
                    {
                        LogManager.LogDebug($"主工作循环检查 - 工序数据数量: {receivedProcessData.Count}, 处理中: {isProcessing}");
                        lastDebugOutput = now;
                    }

                    // 只有在非处理状态下才检查新触发
                    if (!isProcessing)
                    {
                        // 使用新的触发检测方法
                        bool plcTriggered = await commManager.CheckPLCTriggerNew();

                        if (plcTriggered)
                        {
                            LogManager.LogInfo("PLC触发信号检测成功，开始执行尾椎安装流程");

                            isProcessing = true; // 设置处理标志

                            try
                            {
                                // 执行尾椎安装流程
                                await ExecuteTailInstallation();
                            }
                            finally
                            {
                                isProcessing = false; // 确保重置处理标志

                                // 等待一段时间，确保PLC信号已经复位
                                await Task.Delay(1000);
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
                    isProcessing = false; // 异常时重置标志
                    await Task.Delay(1000, cancellationTokenSource.Token);
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
                LogManager.LogInfo($"开始处理接收到的工序数据，长度: {jsonData?.Length ?? 0}");
                
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
                    LogManager.LogInfo($"工序数据已入队: {jsonData.Substring(0, Math.Min(50, jsonData.Length))}...");

                    int count = receivedProcessData.Count;
                    LogManager.LogInfo($"队列中当前工序数据数量: {count}");

                    string statusMessage = "";
                    if (count == 1)
                    {
                        statusMessage = "已接收到第一道工序数据";
                    }
                    else if (count == 2)
                    {
                        statusMessage = "已接收到第二道工序数据";
                    }
                    else if (count == 3)
                    {
                        statusMessage = "已接收到第三道工序数据";
                    }
                    else if (count > 3)
                    {
                        statusMessage = $"已接收 {count} 道工序数据";
                    }
                    else
                    {
                        statusMessage = $"已接收 {count}/3 道工序数据";
                    }

                    LogManager.LogInfo($"状态更新: {statusMessage}");
                    OnProcessStatusChanged?.Invoke("", statusMessage);

                    if (count >= 3)
                    {
                        LogManager.LogInfo("✅ 前三道工序数据已收齐，准备执行尾椎安装");
                        OnProcessStatusChanged?.Invoke("", "工序数据已收齐，等待PLC触发");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理接收数据异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
                OnProcessStatusChanged?.Invoke("", $"数据处理异常: {ex.Message}");
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

        // 拧紧轴数据处理方法 
        private void ProcessTighteningData(TighteningAxisData tighteningData)
        {
            try
            {
                lock (tighteningDataLock)
                {
                    latestTighteningData = tighteningData;
                }

                // 🔧 这里作为主要的日志输出点，保持原有逻辑
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
                var tempList = receivedProcessData.ToList(); // 先转为列表

                for (int i = 0; i < processDataArray.Length && i < 3; i++)
                {
                    processDataArray[i] = tempList[i];
                }

                // 只移除已使用的前3条数据
                for (int i = 0; i < 3 && receivedProcessData.Count > 0; i++)
                {
                    receivedProcessData.TryDequeue(out _);
                }

                LogManager.LogInfo($"获取并移除前3条工序数据，剩余队列数量: {receivedProcessData.Count}");

                return processDataArray;
            }
        }

        private async Task ExecuteTailInstallation()
        {
            var startTime = DateTime.Now;
            try
            {
                // 验证工序数据完整性
                if (!ValidateProcessData())
                {
                    LogManager.LogWarning("工序数据验证失败，无法执行尾椎安装");
                    OnProcessStatusChanged?.Invoke("", "工序数据不完整或无效");
                    await commManager.WritePLCDRegister("D522", 1);
                    return;
                }

                LogManager.LogInfo("开始执行尾椎安装工序");
                OnProcessStatusChanged?.Invoke("", "开始执行尾椎安装");

                // 修改：持续扫码直到成功
                string barcode = await WaitForBarcodeScanWithRetry();

                // 验证条码
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    throw new InvalidOperationException("扫描到的条码为空");
                }

                OnCurrentProductChanged?.Invoke(barcode, "条码扫描完成");
                // 步骤3：通知PLC扫码完成
                LogManager.LogInfo($"通知PLC扫码完成 - D521=1, D501=0");
                await commManager.WritePLCDRegister(_config.PLC.ScanCompleteAddress, 1);  // D521 = 1
                await commManager.WritePLCDRegister(_config.PLC.TriggerAddress, 0);       // D501 = 0
                                                                                          // 步骤4：启动心跳
                LogManager.LogInfo("启动心跳信号");
                await commManager.StartHeartbeat();
                try
                {
                    // 步骤5：等待拧紧轴操作完成
                    OnProcessStatusChanged?.Invoke(barcode, "等待拧紧轴操作完成");
                    var tighteningResult = await WaitForTighteningCompletion();
                    OnCurrentProductChanged?.Invoke(barcode, "拧紧操作完成");
                    // 步骤6：生成本工序数据
                    OnProcessStatusChanged?.Invoke(barcode, "生成工序数据");
                    var tailProcessData = GenerateTailProcessData(barcode, tighteningResult);
                    // 步骤7：整合所有数据
                    OnProcessStatusChanged?.Invoke(barcode, "整合数据");
                    var processDataArray = SafeGetProcessData();
                    var completeData = CombineAllProcessData(tailProcessData, processDataArray);
                    // 步骤8：保存到本地数据库
                    OnProcessStatusChanged?.Invoke(barcode, "保存数据");
                    await dataManager.SaveProductData(barcode, processDataArray, tailProcessData, completeData);
                    // 步骤9：保存到本地JSON文件
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
                    // 步骤10：上传到服务器
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

                    var endTime = DateTime.Now;
                    var duration = (endTime - startTime).TotalSeconds;

                    LogManager.LogInfo("========== 尾椎安装流程完成 ==========");
                    LogManager.LogInfo($"产品条码: {barcode}");
                    LogManager.LogInfo($"总耗时: {duration:F1} 秒");
                    LogManager.LogInfo($"拧紧结果: {(tighteningResult.Success ? "合格" : "不合格")} - {tighteningResult.QualityResult}");
                    LogManager.LogInfo($"完成扭矩: {tighteningResult.Torque:F2}Nm (目标: {tighteningResult.TargetTorque:F2}Nm)");
                    LogManager.LogInfo($"数据上传: {(uploadSuccess ? "成功" : "失败，已加入重试队列")}");
                    LogManager.LogInfo("=====================================");
                }
                finally
                {
                    // 确保停止心跳
                    LogManager.LogInfo("停止心跳信号");
                    commManager.StopHeartbeat();

                    // 复位扫码完成信号
                    await commManager.WritePLCDRegister(_config.PLC.ScanCompleteAddress, 0);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"尾椎安装异常: {ex.Message}");
                OnProcessStatusChanged?.Invoke("", $"安装异常: {ex.Message}");

                // 停止心跳
                commManager.StopHeartbeat();

                // 通知PLC异常
                try
                {
                    await commManager.WritePLCDRegister("D522", 1); // 错误标志
                }
                catch { }
            }
            finally
            {
                OnProcessStatusChanged?.Invoke("", "等待下一个产品");
            }
        }

        /// <summary>
        /// 持续发送"ON"指令直到扫码成功
        /// </summary>
        private async Task<string> WaitForBarcodeScanWithRetry()
        {
            LogManager.LogInfo("开始持续扫码流程...");

            int retryCount = 0;
            const int maxRetries = 60; // 最大重试次数（避免无限循环）
            const int scanInterval = 5000; // 每5秒重试一次

            while (retryCount < maxRetries && GetRunningState() && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    retryCount++;

                    // 步骤1：发送扫码指令
                    OnProcessStatusChanged?.Invoke("", $"发送扫码指令 (第{retryCount}次)");
                    LogManager.LogInfo($"发送扫码指令 - 第{retryCount}次尝试");

                    bool scanCommandSent = await commManager.SendScannerCommand("ON");
                    if (!scanCommandSent)
                    {
                        LogManager.LogWarning($"第{retryCount}次扫码指令发送失败，等待{scanInterval / 1000}秒后重试");
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
                            LogManager.LogInfo($"扫码成功！条码: {barcode} (第{retryCount}次尝试)");
                            OnProcessStatusChanged?.Invoke("", "条码扫描成功");
                            return barcode;
                        }
                    }
                    catch (TimeoutException)
                    {
                        LogManager.LogInfo($"第{retryCount}次扫码超时，继续重试...");
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
                    LogManager.LogError($"第{retryCount}次扫码尝试异常: {ex.Message}");

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

            // 创建等待任务
            TaskCompletionSource<string> waitTask;
            lock (barcodeTaskLock)
            {
                barcodeWaitTask = new TaskCompletionSource<string>();
                waitTask = barcodeWaitTask;
            }

            try
            {
                // 设置较短的超时时间
                using (var timeoutCts = new CancellationTokenSource(timeoutMs))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, cancellationTokenSource.Token))
                {
                    var timeoutTask = Task.Delay(timeoutMs, combinedCts.Token);
                    var completedTask = await Task.WhenAny(waitTask.Task, timeoutTask);

                    if (completedTask == waitTask.Task && !waitTask.Task.IsCanceled)
                    {
                        return waitTask.Task.Result;
                    }
                    else
                    {
                        // 不记录错误，因为这是正常的重试机制
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

            var testItems = new[]
            {
            new
            {
                Id = 37,                      
                ItemName = "尾椎安装至C壳",   
                ItemType = 0                
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
                    pass = tighteningResult.Success,
                    resultText = tighteningResult.QualityResult
                   
                }).ToArray()
            };

            string jsonResult = JsonConvert.SerializeObject(processData, Formatting.Indented);

            LogManager.LogInfo($"尾椎安装工序数据生成完成");
            LogManager.LogInfo($"测试项数量: {testItems.Length}");
            LogManager.LogInfo($"完成扭矩: {tighteningResult.Torque:F2}Nm");
            LogManager.LogInfo($"目标扭矩: {tighteningResult.TargetTorque:F2}Nm");
            LogManager.LogInfo($"测试结果: {(tighteningResult.Success ? "通过" : "失败")} - {tighteningResult.QualityResult}");

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
