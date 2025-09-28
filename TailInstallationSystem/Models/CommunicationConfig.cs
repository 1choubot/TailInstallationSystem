using Newtonsoft.Json;

namespace TailInstallationSystem.Models
{
    public class CommunicationConfig
    {
        public PLCConfig PLC { get; set; } = new PLCConfig();
        public ScannerConfig Scanner { get; set; } = new ScannerConfig();
        public TighteningAxisConfig TighteningAxis { get; set; } = new TighteningAxisConfig();
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

        // 新增：日志优化配置
        public LoggingSettings Logging { get; set; } = new LoggingSettings();
    }

    // 新增：日志配置类
    public class LoggingSettings
    {
        /// <summary>
        /// 是否启用重复日志抑制
        /// </summary>
        public bool EnableDuplicateLogSuppression { get; set; } = true;

        /// <summary>
        /// 重复日志抑制间隔（秒）
        /// </summary>
        public int DuplicateLogSuppressionSeconds { get; set; } = 30;

        /// <summary>
        /// 拧紧轴数据日志级别：None=不记录, Error=仅错误, Warn=警告和错误, Info=所有
        /// </summary>
        public string TighteningAxisLogLevel { get; set; } = "Error";

        /// <summary>
        /// 是否启用详细的设备轮询日志
        /// </summary>
        public bool EnableVerbosePollingLogs { get; set; } = false;

        /// <summary>
        /// 字节序转换警告抑制间隔（秒）
        /// </summary>
        public int ByteOrderWarningSuppressionSeconds { get; set; } = 60;

        /// <summary>
        /// 是否启用状态变化日志（只记录变化，不记录重复状态）
        /// </summary>
        public bool EnableStateChangeLogsOnly { get; set; } = true;

        /// <summary>
        /// 最大重复日志计数（超过此数量后停止记录重复日志）
        /// </summary>
        public int MaxDuplicateLogCount { get; set; } = 3;
    }

    public class PLCConfig
    {
        public string IP { get; set; } = "192.168.1.88";
        public int Port { get; set; } = 502;
        public byte Station { get; set; } = 1;
        public string TriggerAddress { get; set; } = "D501";      // 触发地址
        public string ScanCompleteAddress { get; set; } = "D521";  // 扫码完成
        public string HeartbeatAddress { get; set; } = "D530";     // 心跳地址
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

    public class TighteningAxisConfig
    {
        public string IP { get; set; } = "192.168.0.102";
        public int Port { get; set; } = 502;
        public byte Station { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 10;

        // 拧紧轴特定配置
        public int StatusPollingIntervalMs { get; set; } = 1000; // 从500ms增加到1000ms减少轮询频率
        public int MaxOperationTimeoutSeconds { get; set; } = 1200; // 最大操作超时时间

        // 扭矩范围配置（可选，如果需要在客户端验证）
        public double MinTorque { get; set; } = 0.1;
        public double MaxTorque { get; set; } = 10.0;

        // 关键寄存器地址
        public ModbusRegisterAddresses Registers { get; set; } = new ModbusRegisterAddresses();
    }
    // 拧紧轴Modbus寄存器地址配置
    public class ModbusRegisterAddresses
    {
        // 控制相关
        public int ControlCommand { get; set; } = 5102;    // 控制命令字
        public int RunningStatus { get; set; } = 5100;     // 运行状态
        public int ErrorCode { get; set; } = 5096;         // 错误代码

        // 扭矩相关  
        public int CompletedTorque { get; set; } = 5092;   // 完成扭矩
        public int RealtimeTorque { get; set; } = 5094;    // 实时扭矩
        public int TargetTorque { get; set; } = 5006;      // 目标扭矩
        public int LowerLimitTorque { get; set; } = 5002;  // 判断下限扭矩
        public int UpperLimitTorque { get; set; } = 5004;  // 判断上限扭矩

        // 统计相关
        public int QualifiedCount { get; set; } = 5088;    // 合格数记录
        public int TighteningMode { get; set; } = 5000;    // 紧固模式

        // 角度相关（如果需要）
        public int RealtimeAngle { get; set; } = 5098;     // 实时角度
    }

    public class PCConfig
    {
        public string IP { get; set; } = "192.168.1.100";
        public int Port { get; set; } = 8080;
        public int TimeoutSeconds { get; set; } = 30;
        public int BufferSize { get; set; } = 16384; // 从8192增加到16384避免JSON截断
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

    // 拧紧轴运行状态枚举
    public enum TighteningStatus
    {
        Idle = 0,           // 空闲
        Running = 1,        // 运行中
        Qualified = 10,     // 合格
        TorqueTooLow = 21,  // 小于下限扭矩
        TorqueTooHigh = 22, // 大于上限扭矩  
        TimeoutError = 23,  // 运行过程超上限时间
        AngleTooLow = 24,   // 小于下限角度
        AngleTooHigh = 25,  // 大于上限角度
        Error = 99          // 其他错误
    }
    // 控制命令枚举
    public enum TighteningCommand
    {
        Start = 100,        // 启动命令
        Completed = 0,      // 运行完成后自动变为00
        Stop = 300,         // 停止命令/清除错误
        Loosen = 400,       // 松动螺栓专用
        RotateAngle = 1000, // 旋转指定角度
        JogForward = 3000,  // 点动运行任务
        JogReverse = 3001   // 点动运行反转
    }
}
