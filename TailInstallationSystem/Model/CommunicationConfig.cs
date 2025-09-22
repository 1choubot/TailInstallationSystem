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

        public SystemSettings System { get; set; } = new SystemSettings();
    }

    public class SystemSettings
    {
        public bool AutoStart { get; set; } = false;
        public string LogLevel { get; set; } = "Info";
        public bool EnableAutoReconnect { get; set; } = true;
        public int ReconnectIntervalSeconds { get; set; } = 30;
        public bool ShowTrayIcon { get; set; } = true;
        public string Language { get; set; } = "zh-CN";
        public bool EnableSoundAlert { get; set; } = true;
        public int DataRetentionDays { get; set; } = 30;

        public bool MinimizeToTray { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
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
        public string IP { get; set; } = "192.168.1.150";
        public int Port { get; set; } = 9000;
        public int TimeoutSeconds { get; set; } = 15;
        public int BufferSize { get; set; } = 1024;
        public double MinTorque { get; set; } = 0.5;
        public double MaxTorque { get; set; } = 2.5;
        public string CommandFormat { get; set; } = "ASCII";
    }

    public class PCConfig
    {
        public string IP { get; set; } = "192.168.1.100";
        public int Port { get; set; } = 8080;
        public int TimeoutSeconds { get; set; } = 30;
        public int BufferSize { get; set; } = 4096;
        public bool IsServer { get; set; } = true;
    }

    public class ServerConfig
    {
        public string WebSocketUrl { get; set; } = "ws://192.168.1.100:9001";
        public int RetryIntervalMinutes { get; set; } = 5;
        public int MaxRetryAttempts { get; set; } = 3;
        public string ApiKey { get; set; } = "";
        
        // 成功响应关键词配置
        public string[] SuccessKeywords { get; set; } = new[] { "success", "ok", "received", "完成", "成功" };
    }
}
