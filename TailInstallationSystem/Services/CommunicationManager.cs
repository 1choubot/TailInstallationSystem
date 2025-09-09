using HslCommunication.ModBus;
using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TailInstallationSystem
{
    public class CommunicationManager
    {
        // PLC 通讯 (Modbus TCP)
        private ModbusTcpNet busTcpClient = null;
        private const string PLC_Ip = "192.168.1.88";
        private const int PLC_Port = 502;
        private const byte PLC_Station = 1;

        // 前端PC数据接收 (TCP)
        private const string TCP_Ip = "192.168.1.102";
        private const int TCP_Port = 8888;
        private Socket socket_PC = null;
        private bool connectSuccess_PC = false;

        // 扫码枪通讯 (TCP)
        private const string scanner_TCP_Ip = "192.168.1.129";
        private const int scanner_TCP_Port = 6666;
        private Socket socketCore_Scanner = null;
        private bool connectSuccess_Scanner = false;
        private byte[] buffer_Scanner = new byte[2048];

        // 螺丝机通讯
        private SerialPort serialPort;

        public event Action<string> OnDataReceived;
        public event Action<string> OnBarcodeScanned;
        public event Action<string> OnScrewDataReceived;
        public event Action<bool> OnPLCTrigger;

        public async Task<bool> InitializeConnections()
        {
            try
            {
                // 初始化PLC连接
                await InitializePLC();

                // 初始化PC TCP连接
                await InitializePCConnection();

                // 初始化扫码枪连接
                await InitializeScannerConnection();

                // 初始化螺丝机连接
                InitializeScrewDriver();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"通讯初始化失败: {ex.Message}");
            }
        }

        private async Task InitializePLC()
        {
            busTcpClient = new ModbusTcpNet(PLC_Ip, PLC_Port, PLC_Station);
            var connect = await busTcpClient.ConnectServerAsync();
            if (!connect.IsSuccess)
            {
                throw new Exception($"PLC连接失败: {connect.Message}");
            }
        }

        private async Task InitializePCConnection()
        {
            socket_PC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket_PC.ConnectAsync(TCP_Ip, TCP_Port);
                connectSuccess_PC = true;

                // 启动数据接收
                _ = Task.Run(ReceivePCData);
            }
            catch (Exception ex)
            {
                throw new Exception($"PC TCP连接失败: {ex.Message}");
            }
        }

        private async Task InitializeScannerConnection()
        {
            socketCore_Scanner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socketCore_Scanner.ConnectAsync(scanner_TCP_Ip, scanner_TCP_Port);
                connectSuccess_Scanner = true;

                // 启动扫码数据接收
                _ = Task.Run(ReceiveScannerData);
            }
            catch (Exception ex)
            {
                throw new Exception($"扫码枪连接失败: {ex.Message}");
            }
        }

        private void InitializeScrewDriver()
        {
            // 根据实际螺丝机协议配置
            serialPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.Open();
        }

        private async Task ReceivePCData()
        {
            byte[] buffer = new byte[4096];
            while (connectSuccess_PC && socket_PC.Connected)
            {
                try
                {
                    int received = await socket_PC.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (received > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, received);
                        OnDataReceived?.Invoke(data);
                    }
                }
                catch (Exception ex)
                {
                    // 处理接收异常
                    break;
                }
            }
        }

        private async Task ReceiveScannerData()
        {
            while (connectSuccess_Scanner && socketCore_Scanner.Connected)
            {
                try
                {
                    int received = await socketCore_Scanner.ReceiveAsync(new ArraySegment<byte>(buffer_Scanner), SocketFlags.None);
                    if (received > 0)
                    {
                        string barcode = Encoding.UTF8.GetString(buffer_Scanner, 0, received);
                        OnBarcodeScanned?.Invoke(barcode.Trim());
                    }
                }
                catch (Exception ex)
                {
                    // 处理接收异常
                    break;
                }
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort.ReadExisting();
            OnScrewDataReceived?.Invoke(data);
        }

        public async Task<bool> CheckPLCTrigger()
        {
            var result = await busTcpClient.ReadAsync("D100", 1); // 读取1个长度
            return result.IsSuccess && result.Content[0] == 1;
        }

        public async Task SendPLCConfirmation()
        {
            await busTcpClient.WriteAsync("D100", (short)0);
        }

        public void Dispose()
        {
            busTcpClient?.ConnectClose();
            socket_PC?.Close();
            socketCore_Scanner?.Close();
            serialPort?.Close();
        }
    }
}