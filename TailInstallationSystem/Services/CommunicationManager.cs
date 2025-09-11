using HslCommunication.ModBus;
using System;
using System.Net.Sockets;
using System.Text;
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

        // PLC 通讯 (Modbus TCP)
        private ModbusTcpNet busTcpClient = null;
        private bool connectSuccess_PLC = false;

        // 前端PC数据接收 (TCP)
        private Socket socket_PC = null;
        private bool connectSuccess_PC = false;

        // 扫码枪通讯 (TCP)
        private Socket socketCore_Scanner = null;
        private bool connectSuccess_Scanner = false;
        private byte[] buffer_Scanner = new byte[2048];

        // 螺丝机通讯 (TCP)
        private Socket socket_ScrewDriver = null;
        private bool connectSuccess_ScrewDriver = false;

        #endregion

        #region 事件

        public event Action<string> OnDataReceived;
        public event Action<string> OnBarcodeScanned;
        public event Action<string> OnScrewDataReceived;
        public event Action<bool> OnPLCTrigger;
        public event Action<string, bool> OnDeviceConnectionChanged;

        #endregion

        #region 构造函数和初始化

        public CommunicationManager(CommunicationConfig config = null)
        {
            _config = config ?? ConfigManager.LoadConfig();
            LogManager.LogInfo($"通讯管理器初始化 - PLC: {_config.PLC.IP}:{_config.PLC.Port}");
        }

        public async Task<bool> InitializeConnections()
        {
            try
            {
                LogManager.LogInfo("开始初始化设备连接...");

                // 初始化PLC连接
                bool plcResult = await InitializePLC();
                OnDeviceConnectionChanged?.Invoke("PLC", plcResult);

                // 初始化PC TCP连接
                bool pcResult = await InitializePCConnection();
                OnDeviceConnectionChanged?.Invoke("PC", pcResult);

                // 初始化扫码枪连接
                bool scannerResult = await InitializeScannerConnection();
                OnDeviceConnectionChanged?.Invoke("Scanner", scannerResult);

                // 初始化螺丝机连接
                bool screwResult = await InitializeScrewDriverConnection();
                OnDeviceConnectionChanged?.Invoke("ScrewDriver", screwResult);

                LogManager.LogInfo($"设备连接初始化完成 - PLC:{plcResult}, PC:{pcResult}, 扫码枪:{scannerResult}, 螺丝机:{screwResult}");
                return plcResult; // 至少PLC必须连接成功
            }
            catch (Exception ex)
            {
                LogManager.LogError($"通讯初始化失败: {ex.Message}");
                throw new Exception($"通讯初始化失败: {ex.Message}");
            }
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
                var result = await busTcpClient.WriteAsync(_config.PLC.StartSignalAddress, (short)0);
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
                if (socket_PC != null)
                {
                    socket_PC.Close();
                    socket_PC = null;
                }

                socket_PC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket_PC.ReceiveTimeout = _config.PC.TimeoutSeconds * 1000;
                socket_PC.SendTimeout = _config.PC.TimeoutSeconds * 1000;

                var connectTask = socket_PC.ConnectAsync(_config.PC.IP, _config.PC.Port);
                var timeoutTask = Task.Delay(_config.PC.TimeoutSeconds * 1000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && socket_PC.Connected)
                {
                    connectSuccess_PC = true;
                    LogManager.LogInfo($"PC TCP连接成功: {_config.PC.IP}:{_config.PC.Port}");

                    // 启动数据接收
                    _ = Task.Run(ReceivePCData);
                    return true;
                }
                else
                {
                    connectSuccess_PC = false;
                    LogManager.LogWarning($"PC TCP连接超时: {_config.PC.IP}:{_config.PC.Port}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connectSuccess_PC = false;
                LogManager.LogWarning($"PC TCP连接失败: {ex.Message}");
                return false;
            }
        }

        private async Task ReceivePCData()
        {
            byte[] buffer = new byte[_config.PC.BufferSize];

            while (connectSuccess_PC && socket_PC != null && socket_PC.Connected)
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
                        // 连接已断开
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"接收PC数据异常: {ex.Message}");
                    break;
                }
            }

            connectSuccess_PC = false;
            OnDeviceConnectionChanged?.Invoke("PC", false);
            LogManager.LogWarning("PC数据接收线程已停止");
        }

        #endregion

        #region 扫码枪通讯

        private async Task<bool> InitializeScannerConnection()
        {
            try
            {
                if (socketCore_Scanner != null)
                {
                    socketCore_Scanner.Close();
                    socketCore_Scanner = null;
                }

                socketCore_Scanner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketCore_Scanner.ReceiveTimeout = _config.Scanner.TimeoutSeconds * 1000;
                socketCore_Scanner.SendTimeout = _config.Scanner.TimeoutSeconds * 1000;

                var connectTask = socketCore_Scanner.ConnectAsync(_config.Scanner.IP, _config.Scanner.Port);
                var timeoutTask = Task.Delay(_config.Scanner.TimeoutSeconds * 1000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && socketCore_Scanner.Connected)
                {
                    connectSuccess_Scanner = true;
                    LogManager.LogInfo($"扫码枪连接成功: {_config.Scanner.IP}:{_config.Scanner.Port}");

                    // 启动扫码数据接收
                    _ = Task.Run(ReceiveScannerData);
                    return true;
                }
                else
                {
                    connectSuccess_Scanner = false;
                    LogManager.LogError($"扫码枪连接超时: {_config.Scanner.IP}:{_config.Scanner.Port}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connectSuccess_Scanner = false;
                LogManager.LogError($"扫码枪连接失败: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveScannerData()
        {
            while (connectSuccess_Scanner && socketCore_Scanner != null && socketCore_Scanner.Connected)
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
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"接收扫码数据异常: {ex.Message}");
                    break;
                }
            }

            connectSuccess_Scanner = false;
            OnDeviceConnectionChanged?.Invoke("Scanner", false);
            LogManager.LogWarning("扫码枪数据接收线程已停止");
        }

        #endregion

        #region 螺丝机通讯（仅TCP）

        private async Task<bool> InitializeScrewDriverConnection()
        {
            try
            {
                if (socket_ScrewDriver != null)
                {
                    socket_ScrewDriver.Close();
                    socket_ScrewDriver = null;
                }

                socket_ScrewDriver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket_ScrewDriver.ReceiveTimeout = _config.ScrewDriver.TimeoutSeconds * 1000;
                socket_ScrewDriver.SendTimeout = _config.ScrewDriver.TimeoutSeconds * 1000;

                var connectTask = socket_ScrewDriver.ConnectAsync(_config.ScrewDriver.IP, _config.ScrewDriver.Port);
                var timeoutTask = Task.Delay(_config.ScrewDriver.TimeoutSeconds * 1000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && socket_ScrewDriver.Connected)
                {
                    connectSuccess_ScrewDriver = true;
                    LogManager.LogInfo($"螺丝机TCP连接成功: {_config.ScrewDriver.IP}:{_config.ScrewDriver.Port}");

                    // 启动数据接收
                    _ = Task.Run(ReceiveScrewDriverData);
                    return true;
                }
                else
                {
                    connectSuccess_ScrewDriver = false;
                    LogManager.LogError($"螺丝机TCP连接超时: {_config.ScrewDriver.IP}:{_config.ScrewDriver.Port}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connectSuccess_ScrewDriver = false;
                LogManager.LogError($"螺丝机TCP连接异常: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveScrewDriverData()
        {
            byte[] buffer = new byte[_config.ScrewDriver.BufferSize];

            while (connectSuccess_ScrewDriver && socket_ScrewDriver != null && socket_ScrewDriver.Connected)
            {
                try
                {
                    int received = await socket_ScrewDriver.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (received > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, received);
                        LogManager.LogInfo($"收到螺丝机数据: {data}");
                        OnScrewDataReceived?.Invoke(data);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"接收螺丝机数据异常: {ex.Message}");
                    break;
                }
            }

            connectSuccess_ScrewDriver = false;
            OnDeviceConnectionChanged?.Invoke("ScrewDriver", false);
            LogManager.LogWarning("螺丝机数据接收线程已停止");
        }

        public async Task<bool> SendScrewDriverCommand(string command)
        {
            if (!connectSuccess_ScrewDriver || socket_ScrewDriver == null)
            {
                LogManager.LogWarning("螺丝机未连接，无法发送命令");
                return false;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                await socket_ScrewDriver.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                LogManager.LogInfo($"发送螺丝机TCP命令: {command}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"发送螺丝机命令失败: {ex.Message}");
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

        public async Task<bool> TestScrewDriverConnection()
        {
            return await TestTcpConnection(_config.ScrewDriver.IP, _config.ScrewDriver.Port, "螺丝机");
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
        public bool IsScrewDriverConnected => connectSuccess_ScrewDriver;

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

        #region IDisposable实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // 停止所有连接
                        connectSuccess_PLC = false;
                        connectSuccess_PC = false;
                        connectSuccess_Scanner = false;
                        connectSuccess_ScrewDriver = false;

                        // 关闭PLC连接
                        busTcpClient?.ConnectClose();
                        busTcpClient = null;

                        // 关闭TCP连接
                        socket_PC?.Close();
                        socket_PC = null;

                        socketCore_Scanner?.Close();
                        socketCore_Scanner = null;

                        socket_ScrewDriver?.Close();
                        socket_ScrewDriver = null;

                        LogManager.LogInfo("通讯管理器资源已释放");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError($"释放通讯管理器资源时发生异常: {ex.Message}");
                    }
                }

                _disposed = true;
            }
        }

        ~CommunicationManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
