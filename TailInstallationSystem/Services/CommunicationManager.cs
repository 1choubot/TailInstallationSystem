using HslCommunication.ModBus;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public class CommunicationManager : IDisposable
    {
        #region 私有字段
        private CancellationTokenSource _pollingCts;
        private bool? _lastPLCTriggerState = null;
        private DateTime _lastDetailedLogTime = DateTime.MinValue;
        private readonly TimeSpan _detailedLogInterval = TimeSpan.FromSeconds(10);
        private int _lastD500Value = 0;
        private int _lastD501Value = 0;  // 保存上一次D501的值
        private DateTime _lastD501ChangeTime = DateTime.MinValue;
        private CancellationTokenSource _heartbeatCts = null;
        private CommunicationConfig _config;
        private bool _disposed = false;
        private readonly object _disposeLock = new object();
        private int heartbeatLogCounter = 0;
        private bool _lastOperationCompleted = false;
        private static bool _lastCompletionLogged = false;
        private volatile TaskCompletionSource<bool> _heartbeatTaskCompletion = null;
        private TighteningAxisData _lastCompletedData = null;
        private readonly object _completedDataLock = new object();

        // 自动重连相关
        private System.Threading.Timer _reconnectTimer;
        private bool _isReconnecting = false;
        private readonly object _reconnectLock = new object();

        // 状态防抖
        private volatile bool _lastPLCStatus = false;
        private volatile bool _lastScannerStatus = false;
        private volatile bool _lastTighteningStatus = false;
        private readonly object _statusLock = new object();

        // 心跳状态管理
        private volatile bool _isHeartbeatRunning = false;
        private readonly object _heartbeatLock = new object();

        // 拧紧轴轮询状态追踪
        private int _pollCount = 0;
        private int _lastCommandState = -999;
        private int _lastRunningStatus = -999;

        // 添加缺失的字段声明
        private DateTime _lastTorqueWarningTime = DateTime.MinValue;
        private readonly TimeSpan _torqueWarningInterval = TimeSpan.FromSeconds(30); // 限制警告频率
        private DateTime _lastEventLogTime = DateTime.MinValue;
        private readonly TimeSpan _eventLogInterval = TimeSpan.FromSeconds(5);

        // 取消令牌支持
        private CancellationTokenSource _cancellationTokenSource;

        // PLC 通讯 (Modbus TCP)
        private ModbusTcpNet busTcpClient = null;
        private bool connectSuccess_PLC = false;

        private volatile bool _isDisposing = false;
        private readonly SemaphoreSlim _disposeSemaphore = new SemaphoreSlim(1, 1);

        // 扫码枪通讯 (TCP)
        private Socket socketCore_Scanner = null;
        private bool connectSuccess_Scanner = false;
        private byte[] buffer_Scanner = new byte[2048];

        // 拧紧轴通讯 (Modbus TCP)
        private ModbusTcpNet tighteningAxisClient = null;
        private bool connectSuccess_TighteningAxis = false;
        private System.Threading.Timer statusPollingTimer = null;
        private bool isStatusPolling = false;
        private readonly object tighteningAxisLock = new object();

        //记录是否首次创建和上次配置
        private static bool _firstInstanceCreated = false;
        private static string _lastConfigSummary = "";

        #endregion

        #region 事件

        public event Action<string> OnBarcodeScanned;
        public event Action<TighteningAxisData> OnTighteningDataReceived; 
        public event Action<bool> OnPLCTrigger;
        public event Action<string, bool> OnDeviceConnectionChanged;

        #endregion

        #region 构造函数和初始化

        public CommunicationManager(CommunicationConfig config = null)
        {
            lock (_disposeLock)
            {
                _isDisposing = false;
                _disposed = false;
            }

            _config = config ?? ConfigManager.LoadConfig();
            _cancellationTokenSource = new CancellationTokenSource();

            // 生成配置摘要
            var configSummary = $"PLC:{_config.PLC.IP}:{_config.PLC.Port}|" +
                               $"Scanner:{_config.Scanner.IP}:{_config.Scanner.Port}|" +
                               $"TighteningAxis:{_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}";
            if (!_firstInstanceCreated)
            {
                LogManager.LogInfo($"通讯管理器初始化:");
                LogManager.LogInfo($"  - PLC: {_config.PLC.IP}:{_config.PLC.Port}");
                LogManager.LogInfo($"  - 扫码枪: {_config.Scanner.IP}:{_config.Scanner.Port}");
                LogManager.LogInfo($"  - 拧紧轴: {_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}");
                _firstInstanceCreated = true;
                _lastConfigSummary = configSummary;
            }
            else if (configSummary != _lastConfigSummary)
            {
                LogManager.LogInfo($"通讯管理器配置已更新:");
                CompareAndLogChanges(_lastConfigSummary, configSummary);
                _lastConfigSummary = configSummary;
            }
            else
            {
                LogManager.LogInfo("通讯管理器重新创建（配置未变化）");
            }
        }

        private void CompareAndLogChanges(string oldSummary, string newSummary)
        {
            try
            {
                // 解析旧配置
                var oldParts = new Dictionary<string, string>();
                foreach (var part in oldSummary.Split('|'))
                {
                    var keyValue = part.Split(':');
                    if (keyValue.Length >= 2)
                    {
                        oldParts[keyValue[0]] = string.Join(":", keyValue.Skip(1));
                    }
                }

                // 解析新配置
                var newParts = new Dictionary<string, string>();
                foreach (var part in newSummary.Split('|'))
                {
                    var keyValue = part.Split(':');
                    if (keyValue.Length >= 2)
                    {
                        newParts[keyValue[0]] = string.Join(":", keyValue.Skip(1));
                    }
                }

                // 比较并输出变化
                foreach (var kvp in newParts)
                {
                    if (!oldParts.ContainsKey(kvp.Key) || oldParts[kvp.Key] != kvp.Value)
                    {
                        LogManager.LogInfo($"  - {kvp.Key} 更新为: {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"比较配置变化时出错: {ex.Message}");
            }
        }


        public async Task<bool> InitializeConnections(bool startPolling = true)
        {
            try
            {
                // 防御性检查：确保上次释放已完成
                if (_isDisposing)
                {
                    LogManager.LogWarning($"初始化时检测到 _isDisposing=true，等待释放完成...");

                    int waitCount = 0;
                    while (_isDisposing && waitCount < 30)  // 最多等10秒
                    {
                        await Task.Delay(100);
                        waitCount++;

                        if (waitCount % 10 == 0)
                        {
                            LogManager.LogDebug($"等待释放完成...已等待{waitCount * 100}ms");
                        }
                    }

                    if (_isDisposing)
                    {
                        LogManager.LogError("等待释放超时，强制重置 _isDisposing 标志");
                        lock (_disposeLock)
                        {
                            _isDisposing = false;
                        }
                    }
                }

                LogManager.LogInfo($"开始初始化设备连接... (启动轮询: {startPolling})");
                // 重新创建取消令牌
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // 初始化PLC连接
                bool plcResult = await InitializePLC();
                SafeTriggerDeviceConnectionChanged("PLC", plcResult);

                // 初始化扫码枪连接
                bool scannerResult = await InitializeScannerConnection();
                OnDeviceConnectionChanged?.Invoke("Scanner", scannerResult);

                // 初始化拧紧轴连接
                bool tighteningAxisResult = await InitializeTighteningAxisConnection(startPolling);
                OnDeviceConnectionChanged?.Invoke("TighteningAxis", tighteningAxisResult);

                LogManager.LogInfo($"设备连接初始化完成 - PLC:{plcResult}, 扫码枪:{scannerResult}, 拧紧轴:{tighteningAxisResult}");

                StartAutoReconnect();
                return plcResult; 
            }
            catch (Exception ex)
            {
                LogManager.LogError($"通讯初始化失败: {ex.Message}");
                throw new Exception($"通讯初始化失败: {ex.Message}");
            }
        }
        #endregion

        #region 拧紧轴通讯 

        private async Task<bool> InitializeTighteningAxisConnection(bool startPolling = true)
        {
            try
            {
                LogManager.LogInfo($"开始初始化拧紧轴连接: {_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}");

                if (tighteningAxisClient != null)
                {
                    tighteningAxisClient.ConnectClose();
                    tighteningAxisClient = null;
                }

                tighteningAxisClient = new ModbusTcpNet(_config.TighteningAxis.IP, _config.TighteningAxis.Port, _config.TighteningAxis.Station);
                var connect = await tighteningAxisClient.ConnectServerAsync();

                if (connect.IsSuccess)
                {
                    connectSuccess_TighteningAxis = true;
                    LogManager.LogInfo($"拧紧轴连接成功: {_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}");

                    // 验证连接
                    var testRead = await Task.Run(() =>tighteningAxisClient.ReadInt16("5100", 2)); 

                    if (testRead.IsSuccess)
                    {
                        float statusFloat = ConvertToFloat(testRead.Content);
                        int statusValue = (int)statusFloat;

                        LogManager.LogInfo($"拧紧轴连接验证成功，状态值: {statusValue}");
                    }
                    else
                    {
                        LogManager.LogError($"拧紧轴连接验证失败: {testRead.Message}");
                    }
                    // 根据参数决定是否启动状态轮询
                    if (startPolling)
                    {
                        StartStatusPolling();
                    }
                    else
                    {
                        LogManager.LogInfo("拧紧轴连接成功，但未启动状态轮询（测试模式）");
                    }
                    return true;
                }
                else
                {
                    connectSuccess_TighteningAxis = false;
                    LogManager.LogError($"拧紧轴连接失败: {connect.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connectSuccess_TighteningAxis = false;
                LogManager.LogError($"拧紧轴初始化异常: {ex.Message}");
                return false;
            }
        }


        // 启动状态轮询定时器
        private void StartStatusPolling()
        {
            try
            {
                // 1. 先设置标志为 false，让旧任务自然退出
                isStatusPolling = false;

                // 2. 取消旧令牌
                if (_pollingCts != null)
                {
                    LogManager.LogDebug("取消旧轮询令牌...");
                    _pollingCts.Cancel();

                    // 3. 等待旧任务退出（最多1秒）
                    int waitCount = 0;
                    while (_pollingCts != null && waitCount < 10)
                    {
                        Thread.Sleep(100);
                        waitCount++;
                    }

                    if (_pollingCts != null)
                    {
                        LogManager.LogWarning("旧轮询任务未退出，强制释放令牌");
                        _pollingCts.Dispose();
                    }

                    _pollingCts = null;
                }

                // 4. 短暂延迟，确保旧任务完全退出
                Thread.Sleep(100);

                // 5. 创建新令牌
                _pollingCts = new CancellationTokenSource();
                isStatusPolling = true;

                Task.Run(async () =>
                {
                 
                    var localCts = _pollingCts;  
                    if (localCts == null)
                    {
                        LogManager.LogError("轮询任务启动失败：令牌为null");
                        return;
                    }

                    while (!_isDisposing && isStatusPolling)
                    {
                        if (localCts.IsCancellationRequested)
                        {
                            LogManager.LogDebug("轮询任务被取消");
                            break;
                        }

                        try
                        {
                            await PollTighteningAxisStatus();

                            await Task.Delay(_config.TighteningAxis.StatusPollingIntervalMs, localCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            LogManager.LogDebug("轮询延时被取消，正常退出");
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogError($"拧紧轴轮询异常: {ex.Message}");
                            await Task.Delay(5000);
                        }
                    }

                });

                LogManager.LogInfo($"拧紧轴状态轮询已启动，间隔: {_config.TighteningAxis.StatusPollingIntervalMs}ms");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"启动拧紧轴状态轮询失败: {ex.Message}");
            }
        }


        // 停止状态轮询
        private void StopStatusPolling(bool disconnectClient = false)
        {
            try
            {
                LogManager.LogInfo($"正在停止拧紧轴状态轮询... (断开连接: {disconnectClient})");

                // 先停止轮询标志
                isStatusPolling = false;

                var currentCts = _pollingCts;
                _pollingCts = null; // 先设为 null，避免其他地方继续使用

                if (currentCts != null)
                {
                    try
                    {
                        if (!currentCts.IsCancellationRequested)
                        {
                            currentCts.Cancel();
                            LogManager.LogDebug("轮询取消令牌已发送");
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogDebug("轮询取消令牌已被释放");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"取消轮询任务时异常: {ex.Message}");
                    }

                    try
                    {
                        currentCts.Dispose();
                        LogManager.LogDebug("轮询取消令牌已释放");
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogDebug("轮询取消令牌已被释放");
                    }
                }
                else
                {
                    LogManager.LogDebug("轮询取消令牌已为null，跳过取消操作");
                }

                // 停止定时器
                if (statusPollingTimer != null)
                {
                    statusPollingTimer.Dispose();
                    statusPollingTimer = null;
                }

                // 等待轮询任务结束
                Thread.Sleep(200); // 增加等待时间

                // 断开连接逻辑...
                if (disconnectClient)
                {
                    lock (tighteningAxisLock)
                    {
                        if (tighteningAxisClient != null)
                        {
                            connectSuccess_TighteningAxis = false;
                            try
                            {
                                tighteningAxisClient.ConnectClose();
                                LogManager.LogInfo("拧紧轴连接已关闭");
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogWarning($"关闭拧紧轴连接异常: {ex.Message}");
                            }
                            tighteningAxisClient = null;
                        }
                    }
                }

                LogManager.LogInfo("拧紧轴状态轮询已停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止拧紧轴状态轮询异常: {ex.Message}");
            }
        }
        private async Task PollTighteningAxisStatus()
        {
            _pollCount++;
            if (_pollCount % 50 == 1)
            {
                LogManager.LogDebug($"拧紧轴轮询执行中，第{_pollCount}次");
            }

            if (!connectSuccess_TighteningAxis || !isStatusPolling || tighteningAxisClient == null)
                return;

            try
            {
                var data = await ReadTighteningAxisData();
                if (data != null)
                {
                    bool statusChanged = false;
                    bool isNewCompletion = false;

                    // 1. 检测状态码变化
                    if (data.StatusCode != _lastRunningStatus)
                    {
                        if (_lastRunningStatus != -999)
                        {
                            string oldStatus = GetStatusName(_lastRunningStatus);
                            string newStatus = data.GetStatusDisplayName();

                            LogManager.LogInfo($"拧紧状态 | {oldStatus}→{newStatus} | " +
                                             $"扭矩:{data.CompletedTorque:F2}Nm | " +
                                             $"角度:{data.CompletedAngle:F1}°");
                            statusChanged = true;
                        }

                        _lastRunningStatus = data.StatusCode;
                    }

                    // 精确判断真正的拧紧完成
                    bool currentCompleted = data.IsOperationCompleted;

                    // 判断条件：
                    // 1. 当前是完成状态(11)
                    // 2. 上一次不是完成状态（避免 11→11 重复缓存）
                    // 3. 上一次是空闲状态(0)（基于你的观察：0→11 是拧紧完成）
                    // 4. 必须有有效扭矩数据
                    bool isRealCompletion = currentCompleted &&
                                           !_lastOperationCompleted &&
                                           _lastRunningStatus == 0 &&  // 关键：只接受从0来的
                                           data.CompletedTorque > 0.01f;

                    if (isRealCompletion)
                    {
                        isNewCompletion = true;

                        // 缓存拧紧完成数据
                        lock (_completedDataLock)
                        {
                            _lastCompletedData = data;
                            LogManager.LogInfo($"✓ [轮询缓存] 完成数据已保存 | " +
                                             $"状态:{data.StatusCode}, " +
                                             $"扭矩:{data.CompletedTorque:F2}Nm, " +
                                             $"角度:{data.CompletedAngle:F1}° (原始值)");
                        }

                        // 记录完成日志（只记录一次）
                        if (!_lastCompletionLogged)
                        {
                            var absAngle = Math.Abs(data.CompletedAngle);
                            LogManager.LogInfo($"拧紧完成 | " +
                                             $"扭矩:{data.CompletedTorque:F2}/{data.TargetTorque:F2}Nm | " +
                                             $"角度:{absAngle:F1}° | " +
                                             $"结果:{(data.IsQualified ? "✓合格" : "✗" + data.QualityResult)}");
                            _lastCompletionLogged = true;
                        }
                    }
                    else if (!currentCompleted)
                    {
                        // 状态离开完成状态时，重置标志
                        _lastCompletionLogged = false;
                    }
                    else if (currentCompleted && _lastOperationCompleted)
                    {
                        // 调试日志：记录被跳过的缓存（回零后的11状态）
                        if (_lastRunningStatus == 500 || _lastRunningStatus == 1000 || _lastRunningStatus == 11)
                        {
                            LogManager.LogDebug($"[缓存跳过] 状态:{_lastRunningStatus}→11 (回零状态切换，不缓存)");
                        }
                    }

                    _lastOperationCompleted = currentCompleted;

                    // 3. 只在状态变化或新完成时触发事件
                    if (statusChanged || isNewCompletion)
                    {
                        OnTighteningDataReceived?.Invoke(data);
                    }
                }
                else
                {
                    LogManager.LogWarning("拧紧轴数据读取失败，可能已断开连接");
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                }
            }
            catch (Exception ex)
            {
                if (isStatusPolling && !_isDisposing)
                {
                    LogManager.LogError($"轮询拧紧轴状态异常: {ex.Message}");
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                }
            }
        }


        /// <summary>
        /// 读取拧紧轴数据
        /// </summary>
        public async Task<TighteningAxisData> ReadTighteningAxisData()
        {
            if (!connectSuccess_TighteningAxis || tighteningAxisClient == null)
            {
                LogManager.LogError($"拧紧轴未连接");
                return null;
            }

            try
            {
                var registers = _config.TighteningAxis.Registers;
                var data = new TighteningAxisData();

                // 1. 读取状态码（地址5104，最关键）
                var statusResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.StatusCode.ToString(), 2));

                if (statusResult.IsSuccess)
                {
                    float statusFloat = ConvertToFloat(statusResult.Content);
                    data.StatusCode = (int)statusFloat;

                    LogManager.LogDebug($"状态码(5104): {data.StatusCode}");
                }
                else
                {
                    LogManager.LogError($"读取状态码失败: {statusResult.Message}");
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                    return null;
                }

                // 2. 读取完成扭矩（地址5094）
                var completedTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.CompletedTorque.ToString(), 2));

                if (completedTorqueResult.IsSuccess)
                {
                    data.CompletedTorque = ConvertToFloat(completedTorqueResult.Content);

                    if (data.CompletedTorque > 0.01f)
                    {
                        LogManager.LogDebug($"完成扭矩(5094): {data.CompletedTorque:F2}Nm");
                    }
                }

                // 3. 读取完成角度（地址5102）
                var completedAngleResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.CompletedAngle.ToString(), 2));

                if (completedAngleResult.IsSuccess)
                {
                    data.CompletedAngle = ConvertToFloat(completedAngleResult.Content);
                }

                // 4. 读取目标扭矩（地址5006）
                var targetTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.TargetTorque.ToString(), 2));

                if (targetTorqueResult.IsSuccess)
                {
                    data.TargetTorque = ConvertToFloat(targetTorqueResult.Content);
                }

                // 5. 读取目标角度（地址5032）
                var targetAngleResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.TargetAngle.ToString(), 2));

                if (targetAngleResult.IsSuccess)
                {
                    data.TargetAngle = ConvertToFloat(targetAngleResult.Content);
                }

                // 6. 读取扭矩下限（地址5002）
                var lowerLimitTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.LowerLimitTorque.ToString(), 2));

                if (lowerLimitTorqueResult.IsSuccess)
                {
                    data.LowerLimitTorque = ConvertToFloat(lowerLimitTorqueResult.Content);
                }

                // 7. 读取扭矩上限（地址5004）
                var upperLimitTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.UpperLimitTorque.ToString(), 2));

                if (upperLimitTorqueResult.IsSuccess)
                {
                    data.UpperLimitTorque = ConvertToFloat(upperLimitTorqueResult.Content);
                }

                // 8. 读取角度下限（地址5042）
                var lowerLimitAngleResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.LowerLimitAngle.ToString(), 2));

                if (lowerLimitAngleResult.IsSuccess)
                {
                    data.LowerLimitAngle = ConvertToFloat(lowerLimitAngleResult.Content);
                }

                // 9. 读取角度上限（地址5044）
                var upperLimitAngleResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.UpperLimitAngle.ToString(), 2));

                if (upperLimitAngleResult.IsSuccess)
                {
                    data.UpperLimitAngle = ConvertToFloat(upperLimitAngleResult.Content);
                }

                // 10. 读取合格数量（地址5090）
                var qualifiedCountResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.QualifiedCount.ToString(), 2));

                if (qualifiedCountResult.IsSuccess)
                {
                    float qualifiedFloat = ConvertToFloat(qualifiedCountResult.Content);
                    data.QualifiedCount = (int)qualifiedFloat;
                }

                // 11. 读取反馈速度（地址5100，可选）
                var speedResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.FeedbackSpeed.ToString(), 2));

                if (speedResult.IsSuccess)
                {
                    data.FeedbackSpeed = ConvertToFloat(speedResult.Content);
                }

                // 设置时间戳
                data.Timestamp = DateTime.Now;
                return data;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"读取拧紧轴数据失败: {ex.Message}");
                connectSuccess_TighteningAxis = false;
                OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                return null;
            }
        }


        private float ConvertToFloat(short[] registers)
        {
            if (registers == null || registers.Length < 2)
                return 0f;

            try
            {
                // 拧紧轴格式：寄存器[1]在高位，寄存器[0]在低位
                uint combined = ((uint)(ushort)registers[1] << 16) | (uint)(ushort)registers[0];
                byte[] bytes = BitConverter.GetBytes(combined);
                return BitConverter.ToSingle(bytes, 0);
            }
            catch
            {
                return 0f;
            }
        }


        /// <summary>
        /// 验证扭矩值是否在合理范围内
        /// </summary>
        private bool IsValidTorqueValue(float torque)
        {
            return !float.IsNaN(torque) &&
                   !float.IsInfinity(torque) &&
                   torque >= 0f &&
                   torque <= 100f;  // 根据实际设备调整上限
        }

        /// <summary>
        /// 获取轮询缓存的完成数据（优先使用）
        /// </summary>
        public TighteningAxisData GetCachedCompletedData()
        {
            lock (_completedDataLock)
            {
                if (_lastCompletedData != null)
                {
                    var dataAge = (DateTime.Now - _lastCompletedData.Timestamp).TotalSeconds;

                    if (dataAge <= 10)  // 10秒内的数据认为有效
                    {
                        var cachedData = _lastCompletedData;

                        // 使用后立即清空，防止被后续读取
                        _lastCompletedData = null;

                        // 重置完成标志，允许下次拧紧再缓存
                        _lastOperationCompleted = false;

                        LogManager.LogInfo($"✓ [使用缓存] 数据年龄:{dataAge:F1}秒 | " +
                                         $"状态:{cachedData.StatusCode}, " +
                                         $"扭矩:{cachedData.CompletedTorque:F2}Nm | " +
                                         $"缓存已清空，准备下次拧紧");
                        return cachedData;
                    }
                    else
                    {
                        LogManager.LogWarning($"✗ [缓存过期] 数据年龄:{dataAge:F1}秒，已超时");
                        _lastCompletedData = null; // 过期数据清空
                    }
                }
                else
                {
                    LogManager.LogDebug("[无缓存] _lastCompletedData 为 null");
                }

                return null;
            }
        }


        /// <summary>
        /// 读取已完成的拧紧数据（D501=1时调用）
        /// </summary>
        public async Task<TighteningAxisData> ReadCompletedTighteningData(int maxRetries = 3, int retryDelayMs = 200)
        {
            LogManager.LogInfo("D501触发：拧紧已完成，开始读取结果数据");

            TighteningAxisData data = null;
            int attemptCount = 0;

            while (attemptCount < maxRetries)
            {
                attemptCount++;

                try
                {
                    // 直接读取拧紧轴寄存器数据
                    data = await ReadTighteningAxisData();

                    if (data != null)
                    {
                        // 验证数据有效性
                        if (ValidateTighteningData(data))
                        {
                            LogManager.LogInfo($"拧紧数据读取成功（第{attemptCount}次尝试）| " +
                                              $"扭矩:{data.CompletedTorque:F2}Nm | " +
                                              $"角度:{data.CompletedAngle:F1}° | " +
                                              $"状态:{data.GetStatusDisplayName()} | " +
                                              $"结果:{(data.IsQualified ? "合格" : data.QualityResult)}");
                            return data;
                        }
                        else
                        {
                            LogManager.LogWarning($"拧紧数据验证失败（第{attemptCount}次）| " +
                                                $"状态码:{data.StatusCode}");
                        }
                    }
                    else
                    {
                        LogManager.LogWarning($"拧紧数据读取失败（第{attemptCount}次）- 返回null");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"拧紧数据读取异常（第{attemptCount}次）: {ex.Message}");
                }

                // 如果不是最后一次尝试，等待后重试
                if (attemptCount < maxRetries)
                {
                    LogManager.LogDebug($"等待{retryDelayMs}ms后重试...");
                    await Task.Delay(retryDelayMs);
                }
            }

            // 所有尝试都失败
            LogManager.LogError($"拧紧数据读取失败，已尝试{maxRetries}次");
            return null;
        }

        /// <summary>
        /// 验证拧紧数据有效性（简化版 - 只检查关键条件）
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
            if (absoluteAngle > 3000)
            {
                LogManager.LogWarning($"角度数据异常(绝对值:{absoluteAngle:F1}°)，但不阻止流程");
            }

            // 4. 扭矩必须在合理范围内
            if (data.CompletedTorque > 100)
            {
                LogManager.LogWarning($"扭矩数据超出合理范围({data.CompletedTorque:F2}Nm)");
            }

            LogManager.LogInfo($"拧紧数据验证通过 | " +
                              $"扭矩:{data.CompletedTorque:F2}Nm | " +
                              $"角度:{absoluteAngle:F1}° (原始:{data.CompletedAngle:F1}°) | " +
                              $"状态:{data.GetStatusDisplayName()} | " +
                              $"结果:{data.QualityResult}");

            return true;
        }


        #endregion
        #region PLC通讯 

        private async Task<bool> InitializePLC()
        {
            try
            {
                if (busTcpClient != null)
                {
                    busTcpClient.ConnectClose();
                    busTcpClient = null;
                }

                busTcpClient = new ModbusTcpNet(_config.PLC.IP, _config.PLC.Port, _config.PLC.Station);
                busTcpClient.ConnectTimeOut = 5000;
                busTcpClient.ReceiveTimeOut = 3000;
                busTcpClient.AddressStartWithZero = true;
                busTcpClient.IsStringReverse = false;

                var connect = await busTcpClient.ConnectServerAsync();

                if (connect.IsSuccess)
                {
                    // 验证连接
                    string testAddress = _config.PLC.TighteningTriggerAddress.Replace("D", "");
                    var testResult = await Task.Run(() => busTcpClient.ReadInt16(testAddress, 1));

                    if (testResult.IsSuccess)
                    {
                        connectSuccess_PLC = true;
                        LogManager.LogInfo($"PLC连接成功: {_config.PLC.IP}:{_config.PLC.Port}");

                        // 显式触发状态更新（绕过防抖）
                        try
                        {
                            OnDeviceConnectionChanged?.Invoke("PLC", true);
                            LogManager.LogDebug("PLC连接状态事件已触发");
                        }
                        catch (Exception eventEx)
                        {
                            LogManager.LogWarning($"触发PLC连接事件异常: {eventEx.Message}");
                        }

                        return true;
                    }
                    else
                    {
                        LogManager.LogError($"PLC连接验证失败: {testResult.Message}");
                        busTcpClient.ConnectClose();
                        busTcpClient = null;
                        return false;
                    }
                }
                else
                {
                    LogManager.LogError($"PLC连接失败: {connect.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connectSuccess_PLC = false;
                LogManager.LogError($"PLC初始化异常: {ex.Message}");
                return false;
            }
        }


        #endregion

        #region 新的PLC方法
        /// <summary>
        /// 读取PLC D寄存器
        /// </summary>
        public async Task<int?> ReadPLCDRegister(string address)
        {
            if (!connectSuccess_PLC || busTcpClient == null)
                return null;
            try
            {
                // D寄存器地址处理（去掉D前缀）
                string numericAddress = address.ToUpper().Replace("D", "");

                // 读取16位整数
                var result = await Task.Run(() => busTcpClient.ReadInt16(numericAddress, 1));

                if (result.IsSuccess && result.Content != null && result.Content.Length > 0)
                {
                    return result.Content[0];
                }
                else
                {
                    LogManager.LogError($"读取PLC寄存器失败 - {address}: {result.Message}");
                    if (result.Message.Contains("远程主机强迫关闭") ||
                        result.Message.Contains("目标计算机积极拒绝"))
                    {
                        connectSuccess_PLC = false;
                        SafeTriggerDeviceConnectionChanged("PLC", false);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"读取PLC寄存器异常 - {address}: {ex.Message}");
                connectSuccess_PLC = false;
                SafeTriggerDeviceConnectionChanged("PLC", false);
                return null;
            }
        }
        /// <summary>
        /// 写入PLC D寄存器
        /// </summary>
        public async Task<bool> WritePLCDRegister(string address, int value)
        {
           
            var startTime = DateTime.Now;

            try
            {
                // 检查系统释放状态
                if (_isDisposing || _disposed)
                {
                    LogManager.LogDebug($"[PLC写入] 跳过（系统释放中） | {address}={value}");
                    return false;
                }
                // 检查连接状态
                if (!connectSuccess_PLC)
                {
                    LogManager.LogWarning($"[PLC写入] 失败（未连接） | {address}={value} | connectSuccess=false");
                    return false;
                }
                if (busTcpClient == null)
                {
                    LogManager.LogWarning($"[PLC写入] 失败（客户端为null） | {address}={value}");
                    return false;
                }


                if (busTcpClient != null)
                {
                    try
                    {
                        string testAddress = address.ToUpper().Replace("D", "");
                        var testRead = busTcpClient.ReadInt16(testAddress, 1);

                        if (testRead.IsSuccess && testRead.Content != null)
                        {
                            LogManager.LogInfo($"  - 当前值: {testRead.Content[0]}");
                        }

                        if (!testRead.IsSuccess)
                        {
                            LogManager.LogWarning($"写入前验证失败，尝试重新连接...");
                            busTcpClient.ConnectClose();
                            var reconnect = busTcpClient.ConnectServer();
                            LogManager.LogInfo($"重连结果: IsSuccess={reconnect.IsSuccess}, Message={reconnect.Message}");

                            if (reconnect.IsSuccess)
                            {
                                connectSuccess_PLC = true;
                            }
                            else
                            {
                                connectSuccess_PLC = false;
                                LogManager.LogError("重连失败，终止写入");
                                return false;
                            }
                        }
                    }
                    catch (Exception testEx)
                    {
                        LogManager.LogError($"写入前验证异常: {testEx.Message}");
                    }
                }
                LogManager.LogInfo($"====================");

                // 准备写入
                string numericAddress = address.ToUpper().Replace("D", "");
                LogManager.LogInfo($"写入PLC寄存器 - {address}: {value}");

                // 执行前再次检查
                if (busTcpClient == null)
                {
                    LogManager.LogWarning($"[PLC写入] 执行前客户端变为null | {address}");
                    return false;
                }

                // 执行写入
                HslCommunication.OperateResult result;

                try
                {
                    result = await Task.Run(() => busTcpClient.Write(numericAddress, (short)value));
                }
                catch (NullReferenceException nullEx)
                {
                    LogManager.LogError($"[PLC写入] 客户端空引用 | {address}={value} | 错误:{nullEx.Message}");
                    return false;
                }
                catch (ObjectDisposedException disposeEx)
                {
                    LogManager.LogError($"[PLC写入] 客户端已释放 | {address}={value} | 错误:{disposeEx.Message}");
                    return false;
                }

                // 检查结果
                if (result == null)
                {
                    LogManager.LogError($"[PLC写入] 结果为null | {address}={value}");
                    return false;
                }

                var duration = (DateTime.Now - startTime).TotalMilliseconds;

                if (result.IsSuccess)
                {
                    LogManager.LogInfo($"PLC寄存器写入成功 - {address} = {value} | 耗时:{duration:F0}ms");
                    return true;
                }
                else
                {
                    LogManager.LogError($"PLC寄存器写入失败 - {address}={value} | " +
                    $"错误码:{result.ErrorCode} | " +
                    $"消息:{result.Message} | " +
                    $"耗时:{duration:F0}ms");

                    return false;
                }
            }
            catch (Exception ex)
            {
                var duration = (DateTime.Now - startTime).TotalMilliseconds;
                LogManager.LogError($"写入PLC寄存器异常 - {address}={value} | " +
                                  $"类型:{ex.GetType().Name} | " +
                                  $"错误:{ex.Message} | " +
                                  $"耗时:{duration:F0}ms");
                return false;
            }
        }



        /// <summary>
        /// 检查D500扫码触发信号（边沿检测）
        /// </summary>
        public async Task<bool> CheckScanTrigger()
        {
            if (!connectSuccess_PLC)
                return false;

            try
            {
                var currentValue = await ReadPLCDRegister(_config.PLC.ScanTriggerAddress);

                if (currentValue.HasValue)
                {
                    // 边沿检测：从0变为1时触发
                    if (currentValue.Value == 1 && _lastD500Value == 0)
                    {
                        _lastD500Value = currentValue.Value;
                        LogManager.LogInfo($"检测到扫码触发信号 - {_config.PLC.ScanTriggerAddress}: 0 → 1");
                        return true;
                    }

                    _lastD500Value = currentValue.Value;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"检查扫码触发异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查D501拧紧数据读取触发信号（边沿检测）
        /// </summary>
        public async Task<bool> CheckTighteningTrigger()
        {
            if (!connectSuccess_PLC)
                return false;

            try
            {
                var currentValue = await ReadPLCDRegister(_config.PLC.TighteningTriggerAddress);

                if (currentValue.HasValue)
                {
                    // 边沿检测：从0变为1时触发
                    if (currentValue.Value == 1 && _lastD501Value == 0)
                    {
                        _lastD501Value = currentValue.Value;
                        LogManager.LogInfo($"检测到拧紧数据读取触发 - {_config.PLC.TighteningTriggerAddress}: 0 → 1");
                        return true;
                    }

                    _lastD501Value = currentValue.Value;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"检查拧紧触发异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启动心跳信号（线程安全，防止重复启动）
        /// </summary>
        public async Task StartHeartbeat()
        {
            try
            {
                LogManager.LogInfo($"准备启动心跳信号 - {_config.PLC.HeartbeatAddress}");

                // 1. 先停止旧心跳任务并等待完全退出
                if (_heartbeatCts != null)
                {
                    LogManager.LogDebug("检测到旧心跳任务，正在停止...");

                    try
                    {
                        // 取消旧令牌
                        if (!_heartbeatCts.IsCancellationRequested)
                        {
                            _heartbeatCts.Cancel();
                        }

                        // 等待旧任务完全退出（最多等待2秒）
                        if (_heartbeatTaskCompletion != null)
                        {
                            var waitTask = _heartbeatTaskCompletion.Task;
                            var timeoutTask = Task.Delay(2000);
                            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

                            if (completedTask == waitTask)
                            {
                                LogManager.LogDebug("旧心跳任务已完全退出");
                            }
                            else
                            {
                                LogManager.LogWarning("等待旧心跳任务退出超时（2秒），强制继续");
                            }
                        }

                        _heartbeatCts.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogDebug($"停止旧心跳异常: {ex.Message}");
                    }

                    _heartbeatCts = null;
                    _heartbeatTaskCompletion = null;

                    // 额外等待确保资源释放
                    await Task.Delay(200);
                }

                // 2. 设置运行标志
                lock (_heartbeatLock)
                {
                    _isHeartbeatRunning = true;
                }

                // 3. 创建新的取消令牌和完成标志
                _heartbeatCts = new CancellationTokenSource();
                _heartbeatTaskCompletion = new TaskCompletionSource<bool>();
                var token = _heartbeatCts.Token;

                LogManager.LogInfo($"启动心跳信号 - {_config.PLC.HeartbeatAddress}");
                // 4. 启动心跳任务
                _ = Task.Run(async () =>
                {
                    int heartbeatLogCounter = 0;
                    bool hasWrittenHeartbeat = false;

                    try
                    {
                        LogManager.LogDebug($"心跳任务已启动 - PLC连接: {connectSuccess_PLC}, Disposing: {_isDisposing}");

                        while (!token.IsCancellationRequested && connectSuccess_PLC && !_isDisposing)
                        {
                            try
                            {
                                bool writeSuccess = await WritePLCDRegisterQuiet(_config.PLC.HeartbeatAddress, 1);

                                if (writeSuccess)
                                {
                                    hasWrittenHeartbeat = true;

                                    // 首次写入成功时记录日志
                                    if (heartbeatLogCounter == 0)
                                    {
                                        LogManager.LogInfo($"心跳信号首次写入成功 - D530=1");
                                    }
                                }
                                else if (heartbeatLogCounter == 0)
                                {
                                    // 首次写入失败，记录详细日志
                                    LogManager.LogWarning($"心跳信号首次写入失败 - PLC连接: {connectSuccess_PLC}");
                                }
                                if (heartbeatLogCounter % 10 == 0 && heartbeatLogCounter > 0)
                                {
                                    LogManager.LogDebug($"心跳信号运行中 - 第{heartbeatLogCounter}次");
                                }
                                heartbeatLogCounter++;
                                await Task.Delay(300, token);
                            }
                            catch (OperationCanceledException)
                            {
                                LogManager.LogDebug("心跳任务正常取消");
                                break;
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogError($"心跳发送异常: {ex.Message}");

                                if (_isDisposing || !connectSuccess_PLC)
                                {
                                    break;
                                }

                                try
                                {
                                    await Task.Delay(1000, token);
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                            }
                        }
                        LogManager.LogDebug($"心跳任务主循环退出 - 已写入: {hasWrittenHeartbeat}, 计数: {heartbeatLogCounter}");
                        // 心跳复位逻辑
                        if (hasWrittenHeartbeat)
                        {
                            try
                            {
                                if (!connectSuccess_PLC || busTcpClient == null)
                                {
                                    LogManager.LogDebug("心跳停止：PLC已断开，跳过寄存器复位");
                                    return;
                                }
                                if (busTcpClient != null)
                                {
                                    string numericAddress = _config.PLC.HeartbeatAddress.ToUpper().Replace("D", "");
                                    var result = await Task.Run(() =>
                                    {
                                        try
                                        {
                                            if (busTcpClient == null) return null;
                                            return busTcpClient.Write(numericAddress, (short)0);
                                        }
                                        catch
                                        {
                                            return null;
                                        }
                                    });
                                    if (result != null && result.IsSuccess)
                                    {
                                        LogManager.LogInfo("心跳信号已停止并复位寄存器(D530=0)");
                                    }
                                    else
                                    {
                                        LogManager.LogDebug("心跳复位跳过：PLC连接已断开");
                                    }
                                }
                                else
                                {
                                    LogManager.LogDebug("心跳复位跳过：PLC客户端已释放");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogManager.LogDebug($"心跳寄存器复位异常: {ex.Message}");
                            }
                        }
                        else
                        {
                            LogManager.LogInfo("心跳信号已停止（未写入过心跳，无需复位）");
                        }

                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"心跳任务异常: {ex.Message}");
                    }
                    finally
                    {
                        // 标记任务完成
                        try
                        {
                            _heartbeatTaskCompletion?.TrySetResult(true);
                            LogManager.LogDebug("心跳任务完全退出");
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogDebug($"设置心跳任务完成标志异常: {ex.Message}");
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"启动心跳信号异常: {ex.Message}");
                lock (_heartbeatLock)
                {
                    _isHeartbeatRunning = false;
                }
                throw;
            }
        }

        /// <summary>
        /// 查询心跳是否正在运行
        /// </summary>
        public bool IsHeartbeatRunning()
        {
            lock (_heartbeatLock)
            {
                return _isHeartbeatRunning;
            }
        }

        /// <summary>
        /// 静默写入PLC寄存器（不输出日志，用于心跳）
        /// </summary>
        private async Task<bool> WritePLCDRegisterQuiet(string address, int value)
        {
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;

            try
            {
                string numericAddress = address.ToUpper().Replace("D", "");
                var result = await Task.Run(() => busTcpClient.Write(numericAddress, (short)value));
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// 停止心跳信号（线程安全）
        /// </summary>
        public void StopHeartbeat()
        {
            try
            {
                // 检查状态
                bool wasRunning;
                lock (_heartbeatLock)
                {
                    wasRunning = _isHeartbeatRunning;
                    _isHeartbeatRunning = false;
                }
                if (!wasRunning)
                {
                    LogManager.LogDebug("心跳信号未运行，跳过停止操作");
                    return;
                }
                LogManager.LogInfo("正在停止心跳信号...");
                if (_heartbeatCts != null)
                {
                    try
                    {
                        if (!_heartbeatCts.IsCancellationRequested)
                        {
                            _heartbeatCts.Cancel();
                            LogManager.LogDebug("心跳取消令牌已发送");
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogDebug("心跳取消令牌已被释放");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogWarning($"取消心跳任务时异常: {ex.Message}");
                    }
                    // 同步等待任务完成（最多500ms）
                    if (_heartbeatTaskCompletion != null)
                    {
                        try
                        {
                            bool completed = _heartbeatTaskCompletion.Task.Wait(500);
                            if (completed)
                            {
                                LogManager.LogDebug("心跳任务已确认退出");
                            }
                            else
                            {
                                LogManager.LogWarning("等待心跳任务退出超时（500ms）");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogDebug($"等待心跳任务完成异常: {ex.Message}");
                        }
                    }
                    try
                    {
                        _heartbeatCts.Dispose();
                        _heartbeatCts = null;
                        _heartbeatTaskCompletion = null;
                        LogManager.LogDebug("心跳取消令牌已释放");
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogDebug("心跳取消令牌已被释放");
                    }
                }
                LogManager.LogInfo("心跳信号已停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止心跳信号异常: {ex.Message}");
            }
        }


        /// <summary>
        /// 复位所有PLC信号
        /// </summary>
        public async Task ResetAllPLCSignals()
        {
            LogManager.LogInfo("复位所有PLC信号");

            // 复位拧紧触发信号
            await WritePLCDRegister(_config.PLC.TighteningTriggerAddress, 0);  // D501 = 0

            // 复位扫码触发信号
            await WritePLCDRegister(_config.PLC.ScanTriggerAddress, 0);        // D500 = 0

            // 复位心跳信号
            await WritePLCDRegister(_config.PLC.HeartbeatAddress, 0);          // D530 = 0
        }

        #endregion


        #region 扫码枪通讯 

        private async Task<bool> InitializeScannerConnection()
        {
            try
            {
                // 先确保旧连接完全释放
                if (socketCore_Scanner != null)
                {
                    LogManager.LogInfo("检测到旧的扫码枪连接，先进行清理...");

                    connectSuccess_Scanner = false;

                    try
                    {
                        if (socketCore_Scanner.Connected)
                        {
                            socketCore_Scanner.Shutdown(SocketShutdown.Both);
                        }
                        socketCore_Scanner.Close();
                    }
                    catch (Exception cleanEx)
                    {
                        LogManager.LogDebug($"清理旧扫码枪连接异常: {cleanEx.Message}");
                    }

                    socketCore_Scanner = null;

                    // 等待旧接收任务退出
                    await Task.Delay(300);
                    LogManager.LogInfo("旧扫码枪连接已清理，等待300ms");
                }

                socketCore_Scanner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketCore_Scanner.ReceiveTimeout = _config.Scanner.TimeoutSeconds * 1000;
                socketCore_Scanner.SendTimeout = _config.Scanner.TimeoutSeconds * 1000;

                using (var timeoutCts = new CancellationTokenSource(_config.Scanner.TimeoutSeconds * 1000))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, _cancellationTokenSource.Token))
                {
                    var connectTask = socketCore_Scanner.ConnectAsync(_config.Scanner.IP, _config.Scanner.Port);
                    var timeoutTask = Task.Delay(_config.Scanner.TimeoutSeconds * 1000, combinedCts.Token);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && !connectTask.IsFaulted && socketCore_Scanner.Connected)
                    {
                        // 先设置状态再启动接收任务
                        connectSuccess_Scanner = true;
                        LogManager.LogInfo($"扫码枪连接成功: {_config.Scanner.IP}:{_config.Scanner.Port}");
                        // 延迟启动接收任务，确保状态稳定
                        await Task.Delay(100);

                        // 启动接收任务
                        _ = Task.Run(() => ReceiveScannerData(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                        return true;
                    }
                    else
                    {
                        connectSuccess_Scanner = false;
                        LogManager.LogError($"扫码枪连接超时: {_config.Scanner.IP}:{_config.Scanner.Port}");
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("扫码枪连接初始化被取消");
                return false;
            }
            catch (Exception ex)
            {
                connectSuccess_Scanner = false;
                LogManager.LogError($"扫码枪连接失败: {ex.Message}");
                return false;
            }
        }


        private async Task ReceiveScannerData(CancellationToken cancellationToken)
        {
            try
            {
                // 循环条件增加取消检查
                while (connectSuccess_Scanner &&
                       socketCore_Scanner != null &&
                       socketCore_Scanner.Connected &&
                       !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        int received = await socketCore_Scanner.ReceiveAsync(
                            new ArraySegment<byte>(buffer_Scanner),
                            SocketFlags.None);

                        if (received > 0)
                        {
                            string barcode = Encoding.UTF8.GetString(buffer_Scanner, 0, received).Trim();
                            if (!string.IsNullOrEmpty(barcode))
                            {
                                LogManager.LogInfo($"扫描到条码: {barcode}");
                                OnBarcodeScanned?.Invoke(barcode);
                            }
                        }
                        else
                        {
                            LogManager.LogInfo("扫码枪连接已正常关闭");
                            break;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // 静默处理Dispose异常（正常关闭）
                        LogManager.LogDebug("扫码枪Socket已被释放");
                        break;
                    }
                    catch (SocketException ex)
                    {
                        // 区分取消和真实错误
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogManager.LogDebug("扫码枪接收任务被取消");
                        }
                        else
                        {
                            LogManager.LogError($"扫码枪Socket异常: {ex.Message}");
                        }
                        break;
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                    {
                        // 不记录取消导致的异常
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            LogManager.LogError($"接收扫码数据异常: {ex.Message}");
                        }
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogDebug("扫码枪数据接收任务被正常取消");
            }
            finally
            {
                // 只有在非取消情况下才更新状态
                if (!cancellationToken.IsCancellationRequested)
                {
                    connectSuccess_Scanner = false;
                    OnDeviceConnectionChanged?.Invoke("Scanner", false);
                    LogManager.LogWarning("扫码枪数据接收线程已停止");
                }
                else
                {
                    LogManager.LogDebug("扫码枪接收任务正常退出（系统停止）");
                }
            }
        }


        /// <summary>
        /// 发送扫码指令
        /// </summary>
        public async Task<bool> SendScannerCommand(string command)
        {
            if (!connectSuccess_Scanner || socketCore_Scanner == null)
            {
                LogManager.LogWarning("扫码枪未连接，无法发送指令");
                return false;
            }
            try
            {
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                LogManager.LogInfo($"发送扫码指令: {command}");
                int sent = await socketCore_Scanner.SendAsync(new ArraySegment<byte>(commandBytes), SocketFlags.None);
                if (sent == commandBytes.Length)
                {
                    LogManager.LogInfo($"扫码指令发送成功: {command}");
                    return true;
                }
                else
                {
                    LogManager.LogWarning($"扫码指令发送不完整: 发送{sent}/{commandBytes.Length}字节");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"发送扫码指令异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 连接测试方法

        public async Task<bool> TestPLCConnection()
        {
            try
            {
                using (var testClient = new ModbusTcpNet(_config.PLC.IP, _config.PLC.Port, _config.PLC.Station))
                {
                    var connect = await testClient.ConnectServerAsync();
                    if (connect.IsSuccess)
                    {
                        // 使用新的拧紧触发地址测试连接
                        string address = _config.PLC.TighteningTriggerAddress.Replace("D", "");
                        var readResult = await Task.Run(() => testClient.ReadInt16(address, 1));
                        testClient.ConnectClose();

                        if (readResult.IsSuccess)
                        {
                            LogManager.LogInfo($"PLC测试成功，D501寄存器值: {readResult.Content[0]}");
                            return true;
                        }
                        else
                        {
                            LogManager.LogError($"PLC读取失败: {readResult.Message}");
                            return false;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC连接测试失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestScannerConnection()
        {
            return await TestTcpConnection(_config.Scanner.IP, _config.Scanner.Port, "扫码枪");
        }


        private async Task<bool> TestTcpConnection(string ip, int port, string deviceName)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.ReceiveTimeout = 5000;
                    tcpClient.SendTimeout = 5000;

                    var connectTask = tcpClient.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && !connectTask.IsFaulted && tcpClient.Connected)
                    {
                        LogManager.LogInfo($"{deviceName}连接测试成功: {ip}:{port}");
                        return true;
                    }
                    else
                    {
                        LogManager.LogWarning($"{deviceName}连接测试超时或失败: {ip}:{port}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"{deviceName}连接测试异常: {ex.Message}");
                return false;
            }
        }

        #endregion


        #region 状态查询 

        public bool IsPLCConnected => connectSuccess_PLC;
        public bool IsScannerConnected => connectSuccess_Scanner;
        public bool IsTighteningAxisConnected => connectSuccess_TighteningAxis; 

        public CommunicationConfig GetCurrentConfig()
        {
            return _config;
        }

        public void UpdateConfig(CommunicationConfig newConfig)
        {
            _config = newConfig;
            LogManager.LogInfo("通讯配置已更新");
        }

        #endregion

        #region 辅助方法

        private async Task<TcpClient> AcceptTcpClientWithCancellation(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                var acceptTask = listener.AcceptTcpClientAsync();
                var tcs = new TaskCompletionSource<bool>();

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    var completedTask = await Task.WhenAny(acceptTask, tcs.Task);

                    if (completedTask == acceptTask)
                    {
                        return await acceptTask;
                    }
                    else
                    {
                        listener?.Stop();
                        throw new OperationCanceledException();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region IDisposable实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposing || !disposing)
            {
                return;
            }

            bool lockTaken = false;
            try
            {
                lockTaken = _disposeSemaphore.Wait(5000);
                if (!lockTaken)
                {
                    LogManager.LogError("获取释放信号量超时");
                    return;
                }

                if (_disposed)
                {
                    return;
                }

                _isDisposing = true;

                LogManager.LogInfo("开始释放通讯管理器资源...");

                // 1. 停止自动重连机制
                StopAutoReconnect();

                // 2. 取消所有异步操作
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException) { }

                // 3. 停止心跳信号
                StopHeartbeat();

                // 4. 重置连接状态（防止新操作）
                connectSuccess_PLC = false;
                connectSuccess_Scanner = false;
                connectSuccess_TighteningAxis = false;

                // 5. 重置状态防抖标志
                lock (_statusLock)
                {
                    _lastPLCStatus = false;
                    _lastScannerStatus = false;
                    _lastTighteningStatus = false;
                    LogManager.LogInfo("状态防抖标志已重置");
                }

                // 6. 确保心跳任务完全退出（增加等待时间）
                try
                {
                    Thread.Sleep(2000);  // 从1秒增加到2秒
                }
                catch (ThreadInterruptedException) { }

                // 7. 最后关闭所有连接
                SafeCloseAllConnections();

                // 8. 释放取消令牌
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    _cancellationTokenSource = null;
                }

                LogManager.LogInfo("通讯管理器资源已安全释放");

                LogManager.LogInfo("重置释放标志 _isDisposing → false");
                _isDisposing = false;  
            }
            catch (Exception ex)
            {
                LogManager.LogError($"释放通讯管理器资源时发生异常: {ex.Message}");

                _isDisposing = false;  
            }
            finally
            {
                _disposed = true;

                if (lockTaken)
                {
                    _disposeSemaphore.Release();
                }
            }
        }


        private void SafeCloseAllConnections()
        {
            // 先停止拧紧轴轮询
            try
            {
                isStatusPolling = false;
                StopStatusPolling(true);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止拧紧轴轮询异常: {ex.Message}");
            }

            // PLC连接
            try
            {
                if (busTcpClient != null)
                {
                    busTcpClient.ConnectClose();
                    busTcpClient = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"关闭PLC连接异常: {ex.Message}");
            }

            // 拧紧轴连接
            try
            {
                if (tighteningAxisClient != null)
                {
                    connectSuccess_TighteningAxis = false;
                    tighteningAxisClient.ConnectClose();
                    tighteningAxisClient = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"关闭拧紧轴连接异常: {ex.Message}");
            }

            // Scanner连接优化
            try
            {
                if (socketCore_Scanner != null)
                {
                    // 先设置状态避免接收任务继续运行
                    connectSuccess_Scanner = false;

                    try
                    {
                        if (socketCore_Scanner.Connected)
                        {
                            try
                            {
                                socketCore_Scanner.Shutdown(SocketShutdown.Both);
                            }
                            catch (SocketException shutdownEx)
                            {
                                // Socket可能已断开，忽略此异常
                                LogManager.LogDebug($"Scanner Shutdown异常（正常）: {shutdownEx.SocketErrorCode}");
                            }
                            catch (ObjectDisposedException)
                            {
                                LogManager.LogDebug("Scanner Socket已被释放");
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LogManager.LogDebug($"检查Scanner连接状态异常: {innerEx.Message}");
                    }

                    // 关闭Socket时也增加保护
                    try
                    {
                        socketCore_Scanner.Close();
                    }
                    catch (Exception closeEx)
                    {
                        LogManager.LogDebug($"关闭Scanner Socket异常: {closeEx.Message}");
                    }

                    socketCore_Scanner = null;

                    // 等待接收任务退出
                    Thread.Sleep(200);
                    LogManager.LogInfo("扫码枪连接已关闭（等待200ms）");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"关闭扫码枪连接异常: {ex.Message}");
            }


        }


        ~CommunicationManager()
        {
            Dispose(false);
        }

        #endregion

        #region 自动重连机制

        /// <summary>
        /// 启动自动重连机制
        /// </summary>
        private void StartAutoReconnect()
        {
            try
            {
                if (_config?.System?.EnableAutoReconnect != true)
                {
                    LogManager.LogInfo("自动重连功能已禁用");
                    return;
                }

                var intervalSeconds = _config.System.ReconnectIntervalSeconds;
                if (intervalSeconds < 5)
                {
                    intervalSeconds = 30; // 最小30秒
                }

                _reconnectTimer?.Dispose();
                _reconnectTimer = new System.Threading.Timer(
                    AutoReconnectCallback,
                    null,
                    TimeSpan.FromSeconds(intervalSeconds),
                    TimeSpan.FromSeconds(intervalSeconds));

                LogManager.LogInfo($"自动重连机制已启动，间隔: {intervalSeconds}秒");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"启动自动重连机制失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止自动重连机制
        /// </summary>
        private void StopAutoReconnect()
        {
            try
            {
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
                LogManager.LogInfo("自动重连机制已停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止自动重连机制异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 自动重连回调
        /// </summary>
        private async void AutoReconnectCallback(object state)
        {
            lock (_reconnectLock)
            {
                if (_isReconnecting || _isDisposing)
                {
                    return;
                }
                _isReconnecting = true;
            }

            try
            {
                // 检查并重连PLC
                if (!connectSuccess_PLC)
                {
                    LogManager.LogInfo("尝试重连PLC...");
                    bool plcResult = await InitializePLC();
                    if (plcResult)
                    {
                        LogManager.LogInfo("PLC重连成功");
                        SafeTriggerDeviceConnectionChanged("PLC", true);
                    }
                }

                // 检查并重连扫码枪
                if (!connectSuccess_Scanner)
                {
                    LogManager.LogInfo("尝试重连扫码枪...");
                    bool scannerResult = await InitializeScannerConnection();
                    if (scannerResult)
                    {
                        LogManager.LogInfo("扫码枪重连成功");
                        OnDeviceConnectionChanged?.Invoke("Scanner", true);
                    }
                }

                // 检查并重连拧紧轴
                if (!connectSuccess_TighteningAxis)
                {
                    LogManager.LogInfo("尝试重连拧紧轴...");
                    bool tighteningResult = await InitializeTighteningAxisConnection(true);
                    if (tighteningResult)
                    {
                        LogManager.LogInfo("拧紧轴重连成功");
                        OnDeviceConnectionChanged?.Invoke("TighteningAxis", true);
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.LogError($"自动重连异常: {ex.Message}");
            }
            finally
            {
                lock (_reconnectLock)
                {
                    _isReconnecting = false;
                }
            }
        }

        /// <summary>
        /// 安全触发设备连接状态变化事件（带防抖）
        /// </summary>
        public void SafeTriggerDeviceConnectionChanged(string deviceName, bool isConnected)
        {
            lock (_statusLock)
            {
                bool shouldTrigger = false;

                switch (deviceName.ToUpper())
                {
                    case "PLC":
                        if (_lastPLCStatus != isConnected)
                        {
                            _lastPLCStatus = isConnected;
                            shouldTrigger = true;
                        }
                        break;
                    case "SCANNER":
                        if (_lastScannerStatus != isConnected)
                        {
                            _lastScannerStatus = isConnected;
                            shouldTrigger = true;
                        }
                        break;
                    case "TIGHTENINGAXIS":
                        if (_lastTighteningStatus != isConnected)
                        {
                            _lastTighteningStatus = isConnected;
                            shouldTrigger = true;
                        }
                        break;
                }

                if (shouldTrigger)
                {
                    try
                    {
                        OnDeviceConnectionChanged?.Invoke(deviceName, isConnected);
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"触发设备状态变化事件异常: {ex.Message}");
                    }
                }
            }
        }

        #endregion


        private string GetStatusName(int statusCode)
        {
            switch (statusCode)
            {
                case -999: return "初始";
                case 0: return "空闲";
                case 1: return "运行中";
                case 11: return "合格";
                case 21: return "扭矩过低";
                case 22: return "扭矩过高";
                case 23: return "超时";
                case 24: return "角度过低";
                case 25: return "角度过高";
                case 100: return "执行命令";
                case 500: return "回零中";
                case 1000: return "执行命令";
                default:
                    if (statusCode >= 21 && statusCode <= 30)
                        return $"不合格({statusCode})";
                    return $"状态{statusCode}";
            }
        }


    }
}

