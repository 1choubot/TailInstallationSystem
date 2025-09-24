using HslCommunication.ModBus;
using Newtonsoft.Json;
using System;
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
        private CommunicationConfig _config;
        private bool _disposed = false;
        private readonly object _disposeLock = new object();

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

        #endregion

        #region 事件

        public event Action<string> OnDataReceived;
        public event Action<string> OnBarcodeScanned;
        public event Action<TighteningAxisData> OnTighteningDataReceived; 
        public event Action<bool> OnPLCTrigger;
        public event Action<string, bool> OnDeviceConnectionChanged;

        #endregion

        #region 构造函数和初始化

        public CommunicationManager(CommunicationConfig config = null)
        {
            _config = config ?? ConfigManager.LoadConfig();
            _cancellationTokenSource = new CancellationTokenSource();
            LogManager.LogInfo($"通讯管理器初始化 - PLC: {_config.PLC.IP}:{_config.PLC.Port}, 拧紧轴: {_config.TighteningAxis.IP}:{_config.TighteningAxis.Port}");
        }

        public async Task<bool> InitializeConnections()
        {
            try
            {
                LogManager.LogInfo("开始初始化设备连接...");
                // 重新创建取消令牌
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // 初始化PLC连接
                bool plcResult = await InitializePLC();
                OnDeviceConnectionChanged?.Invoke("PLC", plcResult);

                // 初始化PC TCP连接
                bool pcResult = await InitializePCConnection();
                OnDeviceConnectionChanged?.Invoke("PC", pcResult);

                // 初始化扫码枪连接
                bool scannerResult = await InitializeScannerConnection();
                OnDeviceConnectionChanged?.Invoke("Scanner", scannerResult);

                // 初始化拧紧轴连接
                bool tighteningAxisResult = await InitializeTighteningAxisConnection();
                OnDeviceConnectionChanged?.Invoke("TighteningAxis", tighteningAxisResult);

                LogManager.LogInfo($"设备连接初始化完成 - PLC:{plcResult}, PC:{pcResult}, 扫码枪:{scannerResult}, 拧紧轴:{tighteningAxisResult}");
                return plcResult; // 至少PLC必须连接成功
            }
            catch (Exception ex)
            {
                LogManager.LogError($"通讯初始化失败: {ex.Message}");
                throw new Exception($"通讯初始化失败: {ex.Message}");
            }
        }
        #endregion

        #region 拧紧轴通讯 

        private async Task<bool> InitializeTighteningAxisConnection()
        {
            try
            {
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

                    // 启动状态轮询
                    StartStatusPolling();

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
                StopStatusPolling(); // 先停止已存在的定时器

                statusPollingTimer = new System.Threading.Timer(async _ => await PollTighteningAxisStatus(),
                    null,
                    TimeSpan.FromMilliseconds(_config.TighteningAxis.StatusPollingIntervalMs),
                    TimeSpan.FromMilliseconds(_config.TighteningAxis.StatusPollingIntervalMs));

                isStatusPolling = true;
                LogManager.LogInfo($"拧紧轴状态轮询已启动，间隔: {_config.TighteningAxis.StatusPollingIntervalMs}ms");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"启动拧紧轴状态轮询失败: {ex.Message}");
            }
        }

        // 停止状态轮询
        private void StopStatusPolling()
        {
            try
            {
                isStatusPolling = false;
                statusPollingTimer?.Dispose();
                statusPollingTimer = null;
                LogManager.LogInfo("拧紧轴状态轮询已停止");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"停止拧紧轴状态轮询异常: {ex.Message}");
            }
        }

        // 轮询拧紧轴状态
        private async Task PollTighteningAxisStatus()
        {
            if (!connectSuccess_TighteningAxis || !isStatusPolling || tighteningAxisClient == null)
                return;

            try
            {
                lock (tighteningAxisLock)
                {
                    if (!connectSuccess_TighteningAxis || tighteningAxisClient == null)
                        return;
                }

                // 读取关键状态数据
                var data = await ReadTighteningAxisData();
                if (data != null)
                {
                    // 触发数据接收事件
                    OnTighteningDataReceived?.Invoke(data);

                    // 如果检测到操作完成，记录详细日志
                    if (data.IsOperationCompleted)
                    {
                        LogManager.LogInfo($"拧紧操作完成 - 状态: {data.Status}, 扭矩: {data.CompletedTorque}Nm, 结果: {(data.IsQualified ? "合格" : "不合格")}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"轮询拧紧轴状态异常: {ex.Message}");

                // 连接异常时尝试重连
                if (ex.Message.Contains("连接") || ex.Message.Contains("网络"))
                {
                    connectSuccess_TighteningAxis = false;
                    OnDeviceConnectionChanged?.Invoke("TighteningAxis", false);
                }
            }
        }

        // 读取拧紧轴数据
        public async Task<TighteningAxisData> ReadTighteningAxisData()
        {
            if (!connectSuccess_TighteningAxis || tighteningAxisClient == null)
                return null;

            try
            {
                var registers = _config.TighteningAxis.Registers;
                var data = new TighteningAxisData();

                // 读取控制命令字（判断运行状态）
                var controlCommandResult = await tighteningAxisClient.ReadFloatAsync(registers.ControlCommand.ToString(), 1);
                if (controlCommandResult.IsSuccess)
                {
                    data.ControlCommand = (int)controlCommandResult.Content[0];
                }

                // 读取运行状态
                var statusResult = await tighteningAxisClient.ReadFloatAsync(registers.RunningStatus.ToString(), 1);
                if (statusResult.IsSuccess)
                {
                    data.RunningStatusCode = (int)statusResult.Content[0];
                    data.Status = (TighteningStatus)data.RunningStatusCode;
                }

                // 读取错误代码
                var errorResult = await tighteningAxisClient.ReadFloatAsync(registers.ErrorCode.ToString(), 1);
                if (errorResult.IsSuccess)
                {
                    data.ErrorCode = (int)errorResult.Content[0];
                }

                // 读取完成扭矩
                var completedTorqueResult = await tighteningAxisClient.ReadFloatAsync(registers.CompletedTorque.ToString(), 1);
                if (completedTorqueResult.IsSuccess)
                {
                    data.CompletedTorque = completedTorqueResult.Content[0];
                }

                // 读取实时扭矩
                var realtimeTorqueResult = await tighteningAxisClient.ReadFloatAsync(registers.RealtimeTorque.ToString(), 1);
                if (realtimeTorqueResult.IsSuccess)
                {
                    data.RealtimeTorque = realtimeTorqueResult.Content[0];
                }

                // 读取目标扭矩
                var targetTorqueResult = await tighteningAxisClient.ReadFloatAsync(registers.TargetTorque.ToString(), 1);
                if (targetTorqueResult.IsSuccess)
                {
                    data.TargetTorque = targetTorqueResult.Content[0];
                }

                // 读取扭矩上下限
                var lowerLimitResult = await tighteningAxisClient.ReadFloatAsync(registers.LowerLimitTorque.ToString(), 1);
                if (lowerLimitResult.IsSuccess)
                {
                    data.LowerLimitTorque = lowerLimitResult.Content[0];
                }

                var upperLimitResult = await tighteningAxisClient.ReadFloatAsync(registers.UpperLimitTorque.ToString(), 1);
                if (upperLimitResult.IsSuccess)
                {
                    data.UpperLimitTorque = upperLimitResult.Content[0];
                }

                // 读取合格数记录
                var qualifiedCountResult = await tighteningAxisClient.ReadFloatAsync(registers.QualifiedCount.ToString(), 1);
                if (qualifiedCountResult.IsSuccess)
                {
                    data.QualifiedCount = (int)qualifiedCountResult.Content[0];
                }

                // 设置时间戳
                data.Timestamp = DateTime.Now;

                return data;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"读取拧紧轴数据失败: {ex.Message}");
                return null;
            }
        }

        // 等待拧紧操作完成
        public async Task<TighteningAxisData> WaitForTighteningCompletion(int timeoutSeconds = 30)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            LogManager.LogInfo("等待拧紧操作完成...");

            while (DateTime.Now - startTime < timeout)
            {
                var data = await ReadTighteningAxisData();
                if (data != null && data.IsOperationCompleted)
                {
                    LogManager.LogInfo($"拧紧操作完成，用时: {(DateTime.Now - startTime).TotalSeconds:F1}秒");
                    return data;
                }

                await Task.Delay(100); // 100ms检查一次
            }

            LogManager.LogWarning($"等待拧紧操作完成超时({timeoutSeconds}秒)");
            return null;
        }

        #endregion
        #region PLC通讯 - 保持不变

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
                var connect = await busTcpClient.ConnectServerAsync();

                if (connect.IsSuccess)
                {
                    connectSuccess_PLC = true;
                    LogManager.LogInfo($"PLC连接成功: {_config.PLC.IP}:{_config.PLC.Port}");
                    return true;
                }
                else
                {
                    connectSuccess_PLC = false;
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
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;

            try
            {
                var result = await busTcpClient.ReadAsync(_config.PLC.StartSignalAddress, 1);
                if (result.IsSuccess)
                {
                    bool triggered = result.Content[0] == 1;
                    if (triggered)
                    {
                        OnPLCTrigger?.Invoke(true);
                        LogManager.LogInfo("检测到PLC触发信号");
                    }
                    return triggered;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"检查PLC触发信号失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPLCConfirmation()
        {
            if (!connectSuccess_PLC || busTcpClient == null)
                return false;

            try
            {
                var result = await busTcpClient.WriteAsync(_config.PLC.ConfirmSignalAddress, (short)1);
                if (result.IsSuccess)
                {
                    LogManager.LogInfo("PLC确认信号发送成功");
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
                var result = await busTcpClient.WriteAsync(_config.PLC.ConfirmSignalAddress, (short)0);
                if (result.IsSuccess)
                {
                    LogManager.LogInfo("PLC信号复位成功");
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

        #region PC通讯 

        private async Task<bool> InitializePCConnection()
        {
            try
            {
                if (_config.PC.IsServer)
                {
                    // 服务端模式
                    pcListener = new TcpListener(System.Net.IPAddress.Any, _config.PC.Port);
                    pcListener.Start();
                    pcServerRunning = true;
                    Interlocked.Exchange(ref pcRetryCount, 0);
                    LogManager.LogInfo($"PC服务端监听启动: 端口{_config.PC.Port}");
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
                    //OnDeviceConnectionChanged?.Invoke("PC", true);
                    return true;
                }
                else
                {
                    // 客户端模式
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
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            LogManager.LogWarning($"从 {clientEndpoint} 接收到空数据");
                            return;
                        }
                        try
                        {
                            JsonConvert.DeserializeObject(json);
                            LogManager.LogInfo($"收到PC工序JSON数据: {json.Substring(0, Math.Min(100, json.Length))}...");
                            OnDataReceived?.Invoke(json);
                        }
                        catch (JsonException ex)
                        {
                            LogManager.LogError($"接收到无效的JSON数据: {ex.Message}");
                            LogManager.LogError($"原始数据: {json.Substring(0, Math.Min(200, json.Length))}...");
                        }
                    }
                    else
                    {
                        LogManager.LogWarning($"PC客户端 {clientEndpoint} 发送了空数据");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PC客户端 {clientEndpoint} 数据处理异常: {ex.Message}");
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
                        var readResult = await testClient.ReadAsync("M100", 1);
                        testClient.ConnectClose();
                        return readResult.IsSuccess;
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

        // 拧紧轴连接测试方法
        public async Task<bool> TestTighteningAxisConnection()
        {
            try
            {
                using (var testClient = new ModbusTcpNet(_config.TighteningAxis.IP, _config.TighteningAxis.Port, _config.TighteningAxis.Station))
                {
                    var connect = await testClient.ConnectServerAsync();
                    if (connect.IsSuccess)
                    {
                        // 尝试读取一个关键寄存器来测试通信
                        var readResult = await testClient.ReadFloatAsync(_config.TighteningAxis.Registers.RunningStatus.ToString(), 1);
                        testClient.ConnectClose();
                        return readResult.IsSuccess;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧轴连接测试失败: {ex.Message}");
                return false;
            }
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

            _disposeSemaphore.Wait();
            try
            {
                if (_disposed) return;
                _isDisposing = true;

                LogManager.LogInfo("开始释放通讯管理器资源...");

                _cancellationTokenSource?.Cancel();

                connectSuccess_PLC = false;
                connectSuccess_PC = false;
                connectSuccess_Scanner = false;
                connectSuccess_TighteningAxis = false;

                try
                {
                    Thread.Sleep(1000);
                }
                catch { }

                SafeCloseAllConnections();

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                LogManager.LogInfo("通讯管理器资源已安全释放");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"释放通讯管理器资源时发生异常: {ex.Message}");
            }
            finally
            {
                _disposed = true;
                _disposeSemaphore.Release();
            }
        }

        private void SafeCloseAllConnections()
        {
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
                StopStatusPolling(); // 停止状态轮询

                if (tighteningAxisClient != null)
                {
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
    }
}

