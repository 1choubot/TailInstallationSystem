using HslCommunication.ModBus;
using Newtonsoft.Json;
using System;
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

        // 自动重连相关
        private System.Threading.Timer _reconnectTimer;
        private bool _isReconnecting = false;
        private readonly object _reconnectLock = new object();

        // 状态防抖
        private volatile bool _lastPLCStatus = false;
        private volatile bool _lastScannerStatus = false;
        private volatile bool _lastTighteningStatus = false;
        private volatile bool _lastPCStatus = false;
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

        // 前端PC数据接收 (TCP)
        private Socket socket_PC = null;
        private bool connectSuccess_PC = false;
        private TcpListener pcListener;
        private bool pcServerRunning = false;
        private volatile int pcRetryCount = 0;
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

        public event Action<string> OnDataReceived;
        public event Action<string> OnBarcodeScanned;
        public event Action<TighteningAxisData> OnTighteningDataReceived; 
        public event Action<bool> OnPLCTrigger;
        public event Action<string, bool> OnDeviceConnectionChanged;

        #endregion

        #region 构造函数和初始化

        // 替换整个构造函数：
        public CommunicationManager(CommunicationConfig config = null)
        {
            _config = config ?? ConfigManager.LoadConfig();
            _cancellationTokenSource = new CancellationTokenSource();

            // 生成配置摘要
            var configSummary = $"PLC:{_config.PLC.IP}:{_config.PLC.Port}|" +
                               $"PC:{_config.PC.IP}:{_config.PC.Port}|" +
                               $"Scanner:{_config.Scanner.IP}:{_config.Scanner.Port}|" +
                               $"TighteningAxis:{_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}";

            // 判断是否需要输出详细信息
            if (!_firstInstanceCreated)
            {
                // 首次创建，输出完整信息
                LogManager.LogInfo($"通讯管理器初始化:");
                LogManager.LogInfo($"  - PLC: {_config.PLC.IP}:{_config.PLC.Port}");
                LogManager.LogInfo($"  - PC: {_config.PC.IP}:{_config.PC.Port} (服务端模式: {_config.PC.IsServer})");
                LogManager.LogInfo($"  - 扫码枪: {_config.Scanner.IP}:{_config.Scanner.Port}");
                LogManager.LogInfo($"  - 拧紧轴: {_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}");

                _firstInstanceCreated = true;
                _lastConfigSummary = configSummary;
            }
            else if (configSummary != _lastConfigSummary)
            {
                // 配置有变化，输出变化的内容
                LogManager.LogInfo($"通讯管理器配置已更新:");
                CompareAndLogChanges(_lastConfigSummary, configSummary);
                _lastConfigSummary = configSummary;
            }
            else
            {
                // 配置未变化，只输出简单信息
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
                LogManager.LogInfo($"开始初始化设备连接... (启动轮询: {startPolling})");
                // 重新创建取消令牌
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // 初始化PLC连接
                bool plcResult = await InitializePLC();
                SafeTriggerDeviceConnectionChanged("PLC", plcResult);

                // 初始化PC TCP连接
                bool pcResult = await InitializePCConnection();
                OnDeviceConnectionChanged?.Invoke("PC", pcResult);

                // 初始化扫码枪连接
                bool scannerResult = await InitializeScannerConnection();
                OnDeviceConnectionChanged?.Invoke("Scanner", scannerResult);

                // 初始化拧紧轴连接
                bool tighteningAxisResult = await InitializeTighteningAxisConnection(startPolling);
                OnDeviceConnectionChanged?.Invoke("TighteningAxis", tighteningAxisResult);

                LogManager.LogInfo($"设备连接初始化完成 - PLC:{plcResult}, PC:{pcResult}, 扫码枪:{scannerResult}, 拧紧轴:{tighteningAxisResult}");

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
                    var testRead = await Task.Run(() => tighteningAxisClient.ReadInt16("5100", 1));
                    if (testRead.IsSuccess)
                    {
                        LogManager.LogInfo($"拧紧轴连接验证成功，状态值: {testRead.Content[0]}");
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
                if (_config?.TighteningAxis == null)
                {
                    LogManager.LogError("拧紧轴配置为空，无法启动状态轮询");
                    return;
                }

                // 停止旧的轮询
                if (_pollingCts != null)
                {
                    _pollingCts.Cancel();
                    _pollingCts.Dispose();
                }

                _pollingCts = new CancellationTokenSource();
                isStatusPolling = true;

                Task.Run(async () =>
                {
                    LogManager.LogInfo($"拧紧轴状态轮询任务启动");

                    while (!_isDisposing && isStatusPolling)
                    {
                        // 每次循环开始时检查取消令牌
                        var currentCts = _pollingCts;
                        if (currentCts == null || currentCts.IsCancellationRequested)
                        {
                            LogManager.LogDebug("轮询任务检测到取消信号，退出循环");
                            break;
                        }

                        try
                        {
                            await PollTighteningAxisStatus();

                            var intervalMs = _config?.TighteningAxis?.StatusPollingIntervalMs ?? 500;

                            // 再次检查取消令牌是否仍然有效
                            if (currentCts != null && !currentCts.IsCancellationRequested)
                            {
                                try
                                {
                                    await Task.Delay(intervalMs, currentCts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    LogManager.LogDebug("轮询延时被取消，正常退出");
                                    break;
                                }
                            }
                            else
                            {
                                break; // 取消令牌无效，退出循环
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            LogManager.LogDebug("轮询任务被正常取消");
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogError($"拧紧轴轮询异常: {ex.Message}");

                            // 异常情况下也要检查取消令牌
                            if (currentCts == null || currentCts.IsCancellationRequested)
                            {
                                break;
                            }

                            // 异常后等待一段时间再继续
                            try
                            {
                                await Task.Delay(1000, currentCts?.Token ?? CancellationToken.None);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                    }

                    LogManager.LogInfo("拧紧轴状态轮询任务结束");
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

                    if (data.ControlCommand != _lastCommandState)
                    {
                        if (_lastCommandState != -999)
                        {
                            if (data.ControlCommand == 100 || data.ControlCommand == 0)
                            {
                                string action = data.ControlCommand == 100 ? "启动" : "完成";
                                LogManager.LogDebug($"拧紧命令 | {_lastCommandState}→{data.ControlCommand} | 动作:{action}");
                            }
                            else
                            {
                                LogManager.LogDebug($"拧紧命令 | {_lastCommandState}→{data.ControlCommand}");
                            }
                            statusChanged = true;
                        }

                        _lastCommandState = data.ControlCommand;
                    }

                    if (data.RunningStatusCode != _lastRunningStatus)
                    {
                        string oldStatus = GetStatusName(_lastRunningStatus);
                        string newStatus = data.GetStatusDisplayName();

                        LogManager.LogInfo($"拧紧状态 | {oldStatus}→{newStatus} | 实时扭矩:{data.RealtimeTorque:F2}Nm");

                        _lastRunningStatus = data.RunningStatusCode;
                        statusChanged = true;
                    }

                    // 完成状态检测（只检测从false到true的变化）
                    bool currentCompleted = data.IsOperationCompleted;
                    if (currentCompleted && !_lastOperationCompleted)
                    {
                        isNewCompletion = true;

                        // 只记录一次完成日志
                        if (!_lastCompletionLogged)
                        {
                            LogManager.LogInfo($"拧紧完成 | " +
                                             $"扭矩:{data.CompletedTorque:F2}/{data.TargetTorque:F2}Nm | " +
                                             $"范围:[{data.LowerLimitTorque:F2},{data.UpperLimitTorque:F2}] | " +
                                             $"结果:{(data.IsQualified ? "✓合格" : "✗" + data.QualityResult)}");
                            _lastCompletionLogged = true;
                        }
                    }
                    else if (!currentCompleted)
                    {
                        _lastCompletionLogged = false;
                    }

                    _lastOperationCompleted = currentCompleted;

                    // 只在状态变化或新完成时触发事件
                    if (statusChanged || isNewCompletion)
                    {
                        OnTighteningDataReceived?.Invoke(data);
                    }
                }
                else
                {
                    // 数据为null，判定为连接断开
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
                    // 异常时更新连接状态
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                }
            }
        }
      
        // 读取拧紧轴数据
        public async Task<TighteningAxisData> ReadTighteningAxisData()
        {
            if (!connectSuccess_TighteningAxis || tighteningAxisClient == null)
            {
                LogManager.LogError($"拧紧轴未连接 - connectSuccess:{connectSuccess_TighteningAxis}, client:{tighteningAxisClient != null}");
                return null;
            }

            try
            {
                var registers = _config.TighteningAxis.Registers;
                var data = new TighteningAxisData();

                // 1. 读取控制命令字
                var controlCommandResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.ControlCommand.ToString(), 1));
                if (controlCommandResult.IsSuccess)
                {
                    data.ControlCommand = controlCommandResult.Content[0];
                    LogManager.LogDebug($"控制命令字(5102): {data.ControlCommand}");
                }
                else
                {
                    LogManager.LogError($"读取控制命令字失败: {controlCommandResult.Message}");
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                    return null;
                }

                // 2. 读取运行状态
                var statusResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.RunningStatus.ToString(), 1));
                if (statusResult.IsSuccess)
                {
                    data.RunningStatusCode = statusResult.Content[0];
                    data.Status = (TighteningStatus)data.RunningStatusCode;
                }

                // 3. 读取错误代码
                var errorResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.ErrorCode.ToString(), 1));
                if (errorResult.IsSuccess)
                {
                    data.ErrorCode = errorResult.Content[0];
                    if (data.ErrorCode != 0)
                    {
                        LogManager.LogDebug($"错误代码(5096): {data.ErrorCode}");
                    }
                }

                // 4. 读取完成扭矩
                var completedTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.CompletedTorque.ToString(), 2)); // 读取2个寄存器

                if (completedTorqueResult.IsSuccess)
                {
                    LogManager.LogDebug($"[拧紧轴] 完成扭矩原始数据 | " +
                       $"寄存器5092[0]:{completedTorqueResult.Content[0]} | " +
                       $"寄存器5092[1]:{completedTorqueResult.Content[1]}");

                    data.CompletedTorque = ConvertToFloat(completedTorqueResult.Content);

                    LogManager.LogDebug($"[拧紧轴] 扭矩转换结果 | {data.CompletedTorque:F4}Nm");

                    // 添加数据有效性验证
                    if (!IsValidTorqueValue(data.CompletedTorque))
                    {
                        LogManager.LogWarning($"扭矩值异常: {data.CompletedTorque:E}，尝试字节序转换");
                        data.CompletedTorque = ConvertToFloatWithByteSwap(completedTorqueResult.Content);

                        if (!IsValidTorqueValue(data.CompletedTorque))
                        {
                            LogManager.LogError($"字节序转换后扭矩值仍异常: {data.CompletedTorque:E}，设为0");
                            data.CompletedTorque = 0f;
                        }
                        else
                        {
                            LogManager.LogInfo($"字节序转换成功，扭矩值: {data.CompletedTorque:F2}Nm");
                        }
                    }

                    if (data.CompletedTorque > 0.01f)
                    {
                        LogManager.LogDebug($"完成扭矩(5092): {data.CompletedTorque:F2}Nm");
                    }
                }

                // 5. 读取实时扭矩
                var realtimeTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.RealtimeTorque.ToString(), 2));
                if (realtimeTorqueResult.IsSuccess)
                {
                    data.RealtimeTorque = ConvertToFloat(realtimeTorqueResult.Content);
                    if (!IsValidTorqueValue(data.RealtimeTorque))
                    {
                        data.RealtimeTorque = ConvertToFloatWithByteSwap(realtimeTorqueResult.Content);
                        if (!IsValidTorqueValue(data.RealtimeTorque))
                        {
                            data.RealtimeTorque = 0f;
                        }
                    }
                }

                // 6. 读取目标扭矩
                var targetTorqueResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.TargetTorque.ToString(), 2));
                if (targetTorqueResult.IsSuccess)
                {
                    data.TargetTorque = ConvertToFloat(targetTorqueResult.Content);
                    if (!IsValidTorqueValue(data.TargetTorque))
                    {
                        data.TargetTorque = ConvertToFloatWithByteSwap(targetTorqueResult.Content);
                        if (!IsValidTorqueValue(data.TargetTorque))
                        {
                            data.TargetTorque = 2.0f; // 设置合理的默认值
                        }
                    }
                }

                // 7. 读取扭矩下限
                var lowerLimitResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.LowerLimitTorque.ToString(), 2));
                if (lowerLimitResult.IsSuccess)
                {
                    data.LowerLimitTorque = ConvertToFloat(lowerLimitResult.Content);
                    if (!IsValidTorqueValue(data.LowerLimitTorque))
                    {
                        data.LowerLimitTorque = ConvertToFloatWithByteSwap(lowerLimitResult.Content);
                        if (!IsValidTorqueValue(data.LowerLimitTorque))
                        {
                            data.LowerLimitTorque = 1.8f; // 设置合理的默认值
                        }
                    }
                }

                // 8. 读取扭矩上限
                var upperLimitResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.UpperLimitTorque.ToString(), 2));
                if (upperLimitResult.IsSuccess)
                {
                    data.UpperLimitTorque = ConvertToFloat(upperLimitResult.Content);
                    if (!IsValidTorqueValue(data.UpperLimitTorque))
                    {
                        data.UpperLimitTorque = ConvertToFloatWithByteSwap(upperLimitResult.Content);
                        if (!IsValidTorqueValue(data.UpperLimitTorque))
                        {
                            data.UpperLimitTorque = 2.2f; // 设置合理的默认值
                        }
                    }
                }

                // 9. 读取实时角度
                var realtimeAngleResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.RealtimeAngle.ToString(), 2));
                if (realtimeAngleResult.IsSuccess)
                {
                    data.RealtimeAngle = ConvertToFloat(realtimeAngleResult.Content);
                    if (!IsValidTorqueValue(data.RealtimeAngle)) // 角度也使用相同的验证逻辑
                    {
                        data.RealtimeAngle = ConvertToFloatWithByteSwap(realtimeAngleResult.Content);
                        if (!IsValidTorqueValue(data.RealtimeAngle))
                        {
                            data.RealtimeAngle = 0f;
                        }
                    }
                }

                // 10. 读取合格数记录
                var qualifiedCountResult = await Task.Run(() =>
                    tighteningAxisClient.ReadInt16(registers.QualifiedCount.ToString(), 1));
                if (qualifiedCountResult.IsSuccess)
                {
                    data.QualifiedCount = qualifiedCountResult.Content[0];
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
                // 标准方式：高位在前，低位在后
                uint combined = ((uint)registers[0] << 16) | (uint)(ushort)registers[1];
                byte[] bytes = BitConverter.GetBytes(combined);
                return BitConverter.ToSingle(bytes, 0);
            }
            catch
            {
                return 0f;
            }
        }

        private float ConvertToFloatWithByteSwap(short[] registers)
        {
            if (registers == null || registers.Length < 2)
                return 0f;

            try
            {
                // 字节序交换方式：低位在前，高位在后
                uint combined = ((uint)(ushort)registers[1] << 16) | (uint)(ushort)registers[0];
                byte[] bytes = BitConverter.GetBytes(combined);
                return BitConverter.ToSingle(bytes, 0);
            }
            catch
            {
                return 0f;
            }
        }

        private bool IsValidTorqueValue(float torque)
        {
            // 扭矩值应该在合理范围内：0-50Nm，且不能是NaN或无穷大
            return !float.IsNaN(torque) &&
                   !float.IsInfinity(torque) &&
                   torque >= 0f &&
                   torque <= 50f &&
                   Math.Abs(torque) < 1e10; // 避免科学计数法的异常值
        }

        // 等待拧紧操作完成
        public async Task<TighteningAxisData> WaitForTighteningCompletion(int timeoutSeconds = 30)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            LogManager.LogInfo("等待拧紧操作完成...");

            int lastControlCommand = -1;

            while (DateTime.Now - startTime < timeout)
            {
                var data = await ReadTighteningAxisData();
                if (data != null)
                {
                    // 记录状态变化
                    if (data.ControlCommand != lastControlCommand)
                    {
                        LogManager.LogInfo($"控制命令字变化: {lastControlCommand} → {data.ControlCommand}");
                        lastControlCommand = data.ControlCommand;
                    }

                    // 检测从100变为0（完成）
                    if (lastControlCommand == 100 && data.ControlCommand == 0)
                    {
                        LogManager.LogInfo($"拧紧操作完成，控制命令字已归0");
                        LogManager.LogInfo($"运行状态: {data.RunningStatusCode}, 完成扭矩: {data.CompletedTorque}");
                        return data;
                    }

                    // 或者检测有明确的结果状态
                    if (data.ControlCommand == 0 && data.RunningStatusCode >= 10)
                    {
                        LogManager.LogInfo($"拧紧操作完成，状态码: {data.RunningStatusCode}");
                        return data;
                    }
                }

                await Task.Delay(100);
            }

            LogManager.LogWarning($"等待拧紧操作完成超时({timeoutSeconds}秒)");
            return null;
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
                    // 使用配置的触发地址进行验证（D501 -> 501）
                    string testAddress = _config.PLC.TriggerAddress.Replace("D", "");
                    var testResult = await Task.Run(() => busTcpClient.ReadInt16(testAddress, 1));

                    if (testResult.IsSuccess)
                    {
                        connectSuccess_PLC = true;
                        LogManager.LogInfo($"PLC连接成功: {_config.PLC.IP}:{_config.PLC.Port}");
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


        public async Task<bool> CheckPLCTrigger()
        {
            if (!connectSuccess_PLC)
            {
                return false;
            }
            try
            {
                using (var testClient = new ModbusTcpNet(_config.PLC.IP, _config.PLC.Port, _config.PLC.Station))
                {
                    testClient.ConnectTimeOut = 5000;
                    testClient.ReceiveTimeOut = 3000;
                    testClient.AddressStartWithZero = true;
                    testClient.IsStringReverse = false;

                    var connect = await testClient.ConnectServerAsync();
                    if (connect.IsSuccess)
                    {
                        string originalAddress = _config.PLC.StartSignalAddress.Replace("M", "").Replace("m", "");

                        var result = await Task.Run(() => testClient.ReadInt16(originalAddress, 1));
                        testClient.ConnectClose();
                        if (result.IsSuccess && result.Content != null && result.Content.Length > 0)
                        {
                            short registerValue = result.Content[0];
                            bool triggered = registerValue > 0;
                            // 只在状态变化时输出详细日志
                            if (_lastPLCTriggerState == null || _lastPLCTriggerState.Value != triggered)
                            {
                                LogManager.LogInfo($"  PLC触发状态变化:");
                                LogManager.LogInfo($"   - 配置地址: {_config.PLC.StartSignalAddress}");
                                LogManager.LogInfo($"   - 读取值: {registerValue}");
                                LogManager.LogInfo($"   - 当前状态: {(triggered ? "触发" : "未触发")}");
                                _lastPLCTriggerState = triggered;
                            }
                            // 否则，每隔一段时间输出一次简要日志（可选）
                            else if (DateTime.Now - _lastDetailedLogTime > _detailedLogInterval)
                            {
                                LogManager.LogDebug($"PLC状态检测 - 地址: M{originalAddress}, 值: {registerValue}, 触发: {triggered}");
                                _lastDetailedLogTime = DateTime.Now;
                            }
                            if (triggered)
                            {
                                LogManager.LogInfo($"检测到PLC触发信号！地址: M{originalAddress}, 值: {registerValue}");
                                OnPLCTrigger?.Invoke(true);
                                return true;
                            }
                        }
                        else
                        {
                            // 只在首次失败或长时间后再次输出
                            if (_lastPLCTriggerState != false)
                            {
                                LogManager.LogWarning($"PLC地址 M{originalAddress} ReadInt16读取失败: {result?.Message}");
                                _lastPLCTriggerState = false;
                            }
                        }
                    }
                    else
                    {
                        LogManager.LogWarning($"PLC触发检测连接失败: {connect.Message}");
                        connectSuccess_PLC = false;
                        SafeTriggerDeviceConnectionChanged("PLC", false);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC触发检测异常: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPLCConfirmation()
        {
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;

            try
            {
                string address = _config.PLC.ConfirmSignalAddress.Replace("M", "").Replace("m", "");

                LogManager.LogInfo($"发送PLC确认信号到地址: M{address}");

                var result = await Task.Run(() => busTcpClient.Write(address, (short)1));

                if (result.IsSuccess)
                {
                    LogManager.LogInfo($"PLC确认信号发送成功 - 地址: {_config.PLC.ConfirmSignalAddress}");
                    return true;
                }
                else
                {
                    LogManager.LogError($"PLC确认信号发送失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"发送PLC确认信号异常: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> ResetPLCSignal()
        {
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;

            try
            {
                string address = _config.PLC.ConfirmSignalAddress.Replace("M", "").Replace("m", "");
                
                var result = await Task.Run(() => busTcpClient.Write(address, (short)0));
                
                if (result.IsSuccess)
                {
                    LogManager.LogInfo($"PLC信号复位成功 - 地址: {_config.PLC.ConfirmSignalAddress}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC信号复位失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 新的PLC方法
        /// <summary>
        /// 检查PLC触发（D501边沿检测）
        /// </summary>
        public async Task<bool> CheckPLCTriggerNew()
        {
            if (!connectSuccess_PLC)
                return false;
            try
            {
                // 读取D501寄存器
                var result = await ReadPLCDRegister(_config.PLC.TriggerAddress);

                if (result.HasValue)
                {
                    int currentValue = result.Value;

                    // 边沿检测：从0变为1时触发
                    if (currentValue == 1 && _lastD501Value == 0)
                    {
                        _lastD501Value = currentValue;
                        _lastD501ChangeTime = DateTime.Now;

                        LogManager.LogInfo($"检测到PLC触发信号（上升沿）- {_config.PLC.TriggerAddress}: 0 → 1");
                        OnPLCTrigger?.Invoke(true);
                        return true;
                    }

                    _lastD501Value = currentValue;

                    // 记录状态变化
                    if (DateTime.Now - _lastDetailedLogTime > _detailedLogInterval)
                    {
                        LogManager.LogDebug($"PLC状态 - {_config.PLC.TriggerAddress}: {currentValue}");
                        _lastDetailedLogTime = DateTime.Now;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"检查PLC触发异常: {ex.Message}");
                return false;
            }
        }
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
            if (_isDisposing || _disposed)
            {
                LogManager.LogDebug($"跳过PLC写入（系统正在释放） - {address}: {value}");
                return false;
            }
            
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;
                
            try
            {
                // D寄存器地址处理
                string numericAddress = address.ToUpper().Replace("D", "");

                LogManager.LogInfo($"写入PLC寄存器 - {address}: {value}");

                if (busTcpClient == null)
                {
                    LogManager.LogWarning($"PLC客户端已释放，取消写入 - {address}");
                    return false;
                }

                var result = await Task.Run(() => busTcpClient.Write(numericAddress, (short)value));

                if (result.IsSuccess)
                {
                    LogManager.LogInfo($"PLC寄存器写入成功 - {address} = {value}");
                    return true;
                }
                else
                {
                    LogManager.LogError($"PLC寄存器写入失败 - {address}: {result.Message}");
                    return false;
                }
            }
            catch (ObjectDisposedException)
            {
                LogManager.LogDebug($"PLC客户端已释放 - {address}");
                return false;
            }
            catch (NullReferenceException)
            {
                LogManager.LogDebug($"PLC客户端为空 - {address}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"写入PLC寄存器异常 - {address}: {ex.Message}");
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
            lock (_heartbeatLock)
            {
                if (_isHeartbeatRunning)
                {
                    LogManager.LogWarning("心跳信号已在运行，跳过重复启动");
                    return;
                }
            }
            
            StopHeartbeat();
            
            lock (_heartbeatLock)
            {
                _isHeartbeatRunning = true;
            }
            
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;
            LogManager.LogInfo($"启动心跳信号 - {_config.PLC.HeartbeatAddress}");

            _ = Task.Run(async () =>
            {
                int heartbeatLogCounter = 0;
                bool hasWrittenHeartbeat = false;

                while (!token.IsCancellationRequested && connectSuccess_PLC && !_isDisposing)
                {
                    try
                    {
                        bool writeSuccess = await WritePLCDRegisterQuiet(_config.PLC.HeartbeatAddress, 1);
                        if (writeSuccess)
                        {
                            hasWrittenHeartbeat = true;
                        }

                        if (heartbeatLogCounter % 10 == 0)
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

                // 心跳复位逻辑（循环退出后立即执行）
                if (hasWrittenHeartbeat)
                {
                    try
                    {
                        if (!connectSuccess_PLC || busTcpClient == null)
                        {
                            LogManager.LogDebug("心跳停止：PLC已断开，跳过寄存器复位");
                            return;
                        }

                        // 直接调用底层写入，绕过状态检查
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

            }, token);
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

                try
                {
                    _heartbeatCts.Dispose();
                    _heartbeatCts = null;
                    LogManager.LogDebug("心跳取消令牌已释放");
                }
                catch (ObjectDisposedException)
                {
                    LogManager.LogDebug("心跳取消令牌已被释放");
                }
            }

            LogManager.LogInfo("心跳信号已停止");
        }


        /// <summary>
        /// 复位所有PLC信号
        /// </summary>
        public async Task ResetAllPLCSignals()
        {
            LogManager.LogInfo("复位所有PLC信号");

            await WritePLCDRegister(_config.PLC.TriggerAddress, 0);      // D501 = 0
            await WritePLCDRegister(_config.PLC.ScanCompleteAddress, 0); // D521 = 0
            await WritePLCDRegister(_config.PLC.HeartbeatAddress, 0);    // D530 = 0
        }
        #endregion

        #region PC通讯 

        private async Task<bool> InitializePCConnection()
        {
            try
            {
                if (_config.PC.IsServer)
                {
                    // 服务端模式
                    try
                    {
                        pcListener = new TcpListener(System.Net.IPAddress.Any, _config.PC.Port);
                        pcListener.Start();
                        pcServerRunning = true;
                        Interlocked.Exchange(ref pcRetryCount, 0);
                        LogManager.LogInfo($"PC服务端启动成功: 端口{_config.PC.Port}");
                        
                        // 启动客户端接收任务
                        _ = Task.Run(async () =>
                        {
                            while (pcServerRunning && pcListener != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                try
                                {
                                    var client = await AcceptTcpClientWithCancellation(pcListener, _cancellationTokenSource.Token);
                                    if (client != null)
                                    {
                                        pcRetryCount = 0;
                                        
                                        // 设置PC连接状态为已连接
                                        connectSuccess_PC = true;
                                        OnDeviceConnectionChanged?.Invoke("PC", true);
                                        
                                        _ = Task.Run(() => HandlePCClient(client), _cancellationTokenSource.Token);
                                    }
                                }
                                catch (ObjectDisposedException)
                                {
                                    LogManager.LogInfo("PC监听器已释放，停止接受连接");
                                    break;
                                }
                                catch (InvalidOperationException)
                                {
                                    LogManager.LogInfo("PC监听器已停止，退出监听循环");
                                    break;
                                }
                                catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                                {
                                    LogManager.LogError($"接受PC客户端连接异常: {ex.Message}");
                                    Interlocked.Increment(ref pcRetryCount);
                                    if (pcRetryCount > 10)
                                    {
                                        LogManager.LogError("PC连接错误次数过多，停止监听");
                                        break;
                                    }
                                    if (pcServerRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        var delayMs = Math.Min(10000, 1000 * pcRetryCount);
                                        await Task.Delay(delayMs, _cancellationTokenSource.Token);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    LogManager.LogInfo("PC监听任务被取消");
                                    break;
                                }
                            }
                            LogManager.LogInfo("PC服务端监听循环已退出");
                        }, _cancellationTokenSource.Token);
                        
                        // 服务端状态
                        LogManager.LogInfo($"PC服务端运行中... 新连接会被自动处理");
                        
                        // 服务端启动成功就返回true
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            LogManager.LogError($"PC端口 {_config.PC.Port} 被占用，无法启动服务端");
                        }
                        else
                        {
                            LogManager.LogError($"PC服务端启动失败: {ex.Message}");
                        }
                        return false;
                    }
                }
                else
                {
                    // 客户端模式 - 保持原有逻辑
                    if (socket_PC != null)
                    {
                        try { socket_PC.Close(); } catch { }
                        socket_PC = null;
                    }
                    socket_PC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket_PC.ReceiveTimeout = _config.PC.TimeoutSeconds * 1000;
                    socket_PC.SendTimeout = _config.PC.TimeoutSeconds * 1000;
                    using (var timeoutCts = new CancellationTokenSource(_config.PC.TimeoutSeconds * 1000))
                    using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        timeoutCts.Token, _cancellationTokenSource.Token))
                    {
                        var connectTask = socket_PC.ConnectAsync(_config.PC.IP, _config.PC.Port);
                        var timeoutTask = Task.Delay(_config.PC.TimeoutSeconds * 1000, combinedCts.Token);
                        var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                        if (completedTask == connectTask && !connectTask.IsFaulted && socket_PC.Connected)
                        {
                            connectSuccess_PC = true;
                            LogManager.LogInfo($"PC TCP连接成功: {_config.PC.IP}:{_config.PC.Port}");
                            _ = Task.Run(() => ReceivePCData(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                            return true;
                        }
                        else
                        {
                            connectSuccess_PC = false;
                            LogManager.LogWarning($"PC TCP连接超时: {_config.PC.IP}:{_config.PC.Port}");
                            return false;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("PC连接初始化被取消");
                return false;
            }
            catch (Exception ex)
            {
                connectSuccess_PC = false;
                LogManager.LogWarning($"PC TCP连接失败: {ex.Message}");
                return false;
            }
        }

        private void HandlePCClient(TcpClient client)
        {
            string clientEndpoint = "未知客户端";
            try
            {
                clientEndpoint = client?.Client?.RemoteEndPoint?.ToString() ?? "未知客户端";
                LogManager.LogInfo($"PC客户端已连接: {clientEndpoint}");

                if (client == null)
                {
                    LogManager.LogWarning("接收到空的客户端连接");
                    return;
                }

                using (client)
                using (var stream = client.GetStream())
                {
                    byte[] buffer = new byte[_config.PC.BufferSize];
                    client.ReceiveTimeout = _config.PC.TimeoutSeconds * 1000;
                    client.SendTimeout = _config.PC.TimeoutSeconds * 1000;

                    // 使用循环读取完整数据
                    var receivedData = new List<byte>();
                    int totalBytesRead = 0;
                    var timeout = DateTime.Now.AddSeconds(_config.PC.TimeoutSeconds);

                    while (DateTime.Now < timeout)
                    {
                        if (stream.DataAvailable)
                        {
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                receivedData.AddRange(buffer.Take(bytesRead));
                                totalBytesRead += bytesRead;

                                // 检查是否接收到完整的JSON（简单检查：以}结尾）
                                string currentData = Encoding.UTF8.GetString(receivedData.ToArray());
                                if (currentData.TrimEnd().EndsWith("}"))
                                {
                                    break; // 接收到完整JSON
                                }
                            }
                            else
                            {
                                break; // 连接关闭
                            }
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(10); // 短暂等待
                        }
                    }

                    if (totalBytesRead > 0)
                    {
                        string json = Encoding.UTF8.GetString(receivedData.ToArray());

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            LogManager.LogWarning($"从 {clientEndpoint} 接收到空数据");
                            return;
                        }

                        // 增强JSON验证
                        if (!IsValidJsonString(json))
                        {
                            LogManager.LogError($"从 {clientEndpoint} 接收到无效JSON格式数据");
                            LogManager.LogError($"数据长度: {json.Length}, 前100字符: {json.Substring(0, Math.Min(100, json.Length))}");
                            return;
                        }

                        try
                        {
                            // 验证JSON格式
                            var testParse = JsonConvert.DeserializeObject(json);
                            LogManager.LogInfo($"收到PC工序JSON数据: {json.Substring(0, Math.Min(100, json.Length))}...");

                            LogManager.LogInfo($"准备触发 OnDataReceived 事件，订阅者数量: {OnDataReceived?.GetInvocationList()?.Length ?? 0}");
                            OnDataReceived?.Invoke(json);
                            LogManager.LogInfo("OnDataReceived 事件已触发");
                        }
                        catch (JsonException ex)
                        {
                            LogManager.LogError($"JSON解析失败: {ex.Message}");
                            LogManager.LogError($"原始数据: {json}");

                            // 尝试修复JSON（去除可能的控制字符）
                            string cleanedJson = CleanJsonString(json);
                            if (cleanedJson != json && IsValidJsonString(cleanedJson))
                            {
                                LogManager.LogInfo("JSON清理后重新解析");
                                OnDataReceived?.Invoke(cleanedJson);
                            }
                        }
                    }
                    else
                    {
                        LogManager.LogWarning($"PC客户端 {clientEndpoint} 发送了空数据或超时");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PC客户端 {clientEndpoint} 数据处理异常: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");

                if (ex is OutOfMemoryException || ex is StackOverflowException)
                {
                    LogManager.LogError("严重系统异常，需要立即处理");
                    throw;
                }
            }
            finally
            {
                LogManager.LogInfo($"PC客户端 {clientEndpoint} 连接已关闭");
            }
        }

        // 新增辅助方法
        private bool IsValidJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            json = json.Trim();

            // 基本格式检查
            if (!(json.StartsWith("{") && json.EndsWith("}")) &&
                !(json.StartsWith("[") && json.EndsWith("]")))
            {
                return false;
            }

            try
            {
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string CleanJsonString(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json;

            // 移除可能的控制字符和无效字符
            var cleaned = new StringBuilder();
            foreach (char c in json)
            {
                if (c >= 32 || c == '\t' || c == '\n' || c == '\r')
                {
                    cleaned.Append(c);
                }
            }

            return cleaned.ToString().Trim();
        }

        private async Task ReceivePCData(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[_config.PC.BufferSize];
            try
            {
                while (connectSuccess_PC && socket_PC != null && socket_PC.Connected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        int received = await socket_PC.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (received > 0)
                        {
                            string data = Encoding.UTF8.GetString(buffer, 0, received);
                            LogManager.LogInfo($"收到PC数据: {data.Substring(0, Math.Min(100, data.Length))}...");
                            OnDataReceived?.Invoke(data);
                        }
                        else
                        {
                            LogManager.LogInfo("PC连接已正常关闭");
                            break;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        LogManager.LogInfo("PC Socket已被释放");
                        break;
                    }
                    catch (SocketException ex)
                    {
                        LogManager.LogError($"PC Socket异常: {ex.Message}");
                        break;
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                    {
                        LogManager.LogError($"接收PC数据异常: {ex.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("PC数据接收任务被取消");
            }
            finally
            {
                connectSuccess_PC = false;
                OnDeviceConnectionChanged?.Invoke("PC", false);
                LogManager.LogWarning("PC数据接收线程已停止");
            }
        }

        #endregion

        #region 扫码枪通讯 

        private async Task<bool> InitializeScannerConnection()
        {
            try
            {
                if (socketCore_Scanner != null)
                {
                    try { socketCore_Scanner.Close(); } catch { }
                    socketCore_Scanner = null;
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
                        connectSuccess_Scanner = true;
                        LogManager.LogInfo($"扫码枪连接成功: {_config.Scanner.IP}:{_config.Scanner.Port}");
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
                while (connectSuccess_Scanner && socketCore_Scanner != null && socketCore_Scanner.Connected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        int received = await socketCore_Scanner.ReceiveAsync(new ArraySegment<byte>(buffer_Scanner), SocketFlags.None);
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
                        LogManager.LogInfo("扫码枪Socket已被释放");
                        break;
                    }
                    catch (SocketException ex)
                    {
                        LogManager.LogError($"扫码枪Socket异常: {ex.Message}");
                        break;
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
                    {
                        LogManager.LogError($"接收扫码数据异常: {ex.Message}");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogManager.LogInfo("扫码枪数据接收任务被取消");
            }
            finally
            {
                connectSuccess_Scanner = false;
                OnDeviceConnectionChanged?.Invoke("Scanner", false);
                LogManager.LogWarning("扫码枪数据接收线程已停止");
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
                        string address = _config.PLC.TriggerAddress.Replace("D", "");

                        var readResult = await Task.Run(() => testClient.ReadInt16(address, 1));
                        testClient.ConnectClose();
                        
                        if (readResult.IsSuccess)
                        {
                            LogManager.LogInfo($"PLC测试成功，{_config.PLC.StartSignalAddress}保持寄存器值: {readResult.Content[0]}");
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

        public async Task<bool> TestPCConnection()
        {
            return await TestTcpConnection(_config.PC.IP, _config.PC.Port, "PC");
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
        public bool IsPCConnected => connectSuccess_PC;
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
            if (_isDisposing || !disposing) return;

            bool lockTaken = false;
            try
            {
                lockTaken = _disposeSemaphore.Wait(5000);
                if (!lockTaken)
                {
                    LogManager.LogError("获取释放信号量超时");
                    return;
                }

                if (_disposed) return;
                _isDisposing = true; 

                LogManager.LogInfo("开始释放通讯管理器资源...");

                StopAutoReconnect();
                // 1.先停止心跳（心跳任务会检测 _isDisposing）
                StopHeartbeat();

                // 2. 取消所有任务
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException) { }

                // 3. 重置连接状态（防止新操作）
                connectSuccess_PLC = false;
                connectSuccess_PC = false;
                connectSuccess_Scanner = false;
                connectSuccess_TighteningAxis = false;

                // 4.确保心跳任务完全退出
                try
                {
                    Thread.Sleep(2000);  // 从1秒增加到2秒
                }
                catch (ThreadInterruptedException) { }

                // 5. 最后关闭连接
                SafeCloseAllConnections();

                // 6. 释放取消令牌
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
            }
            catch (Exception ex)
            {
                LogManager.LogError($"释放通讯管理器资源时发生异常: {ex.Message}");
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
                isStatusPolling = false;  // 立即停止轮询标记
                StopStatusPolling(true); // 停止状态轮询
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

            // 拧紧轴连接 - 确保完全关闭
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

            // PC连接
            try
            {
                if (pcListener != null)
                {
                    pcServerRunning = false;
                    pcListener.Stop();
                    pcListener = null;
                }

                if (socket_PC != null)
                {
                    if (socket_PC.Connected)
                    {
                        try
                        {
                            socket_PC.Shutdown(SocketShutdown.Both);
                            Thread.Sleep(100);
                        }
                        catch { }
                    }
                    socket_PC.Close();
                    socket_PC = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"关闭PC连接异常: {ex.Message}");
            }

            // 扫码枪连接
            try
            {
                if (socketCore_Scanner != null)
                {
                    if (socketCore_Scanner.Connected)
                    {
                        try
                        {
                            socketCore_Scanner.Shutdown(SocketShutdown.Both);
                            Thread.Sleep(100);
                        }
                        catch { }
                    }
                    socketCore_Scanner.Close();
                    socketCore_Scanner = null;
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
                    case "PC":
                        if (_lastPCStatus != isConnected)
                        {
                            _lastPCStatus = isConnected;
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
                case 10: return "合格";
                case 21: return "扭矩过低";
                case 22: return "扭矩过高";
                case 23: return "超时";
                case 24: return "角度过低";
                case 25: return "角度过高";
                default: return $"状态{statusCode}";
            }
        }

    }
}

