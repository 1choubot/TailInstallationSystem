// Models/CommunicationConfig.cs
using Newtonsoft.Json;

namespace TailInstallationSystem.Models
{
    public class CommunicationConfig
    {
        public PLCConfig PLC { get; set; } = new PLCConfig();
        public ScannerConfig Scanner { get; set; } = new ScannerConfig();
        public ScrewDriverConfig ScrewDriver { get; set; } = new ScrewDriverConfig();
        public PCConfig PC { get; set; } = new PCConfig();
        public ServerConfig Server { get; set; } = new ServerConfig();
    }

    public class PLCConfig
    {
        public string IP { get; set; } = "192.168.1.88";
        public int Port { get; set; } = 502;
        public byte Station { get; set; } = 1;
        public string StartSignalAddress { get; set; } = "M100";
        public string ConfirmSignalAddress { get; set; } = "M101";
    }

    public class ScannerConfig
    {
        public string IP { get; set; } = "192.168.1.129";
        public int Port { get; set; } = 6666;
        public int TimeoutSeconds { get; set; } = 10;
        public int BufferSize { get; set; } = 2048;
    }

    public class ScrewDriverConfig
    {
        public string IP { get; set; } = "192.168.1.150";    // 螺丝机TCP IP
        public int Port { get; set; } = 9000;                // 螺丝机TCP端口
        public int TimeoutSeconds { get; set; } = 15;
        public int BufferSize { get; set; } = 1024;
        public double MinTorque { get; set; } = 0.5;         // 最小扭矩
        public double MaxTorque { get; set; } = 2.5;         // 最大扭矩
        public string CommandFormat { get; set; } = "ASCII"; // 命令格式
    }

    public class PCConfig
    {
        public string IP { get; set; } = "192.168.1.100";    // PC TCP IP
        public int Port { get; set; } = 8080;                // PC TCP端口
        public int TimeoutSeconds { get; set; } = 30;
        public int BufferSize { get; set; } = 4096;
        public bool IsServer { get; set; } = true;           // 是否作为服务器端
    }

    public class ServerConfig
    {
        public string WebSocketUrl { get; set; } = "ws://192.168.1.200:9090/ws";
        public int RetryIntervalMinutes { get; set; } = 5;
        public int MaxRetryAttempts { get; set; } = 3;
        public string ApiKey { get; set; } = "";
    }
}
