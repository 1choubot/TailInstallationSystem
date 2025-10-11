using Newtonsoft.Json;
using System;

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

        public LoggingSettings Logging { get; set; } = new LoggingSettings();

        public WorkMode CurrentWorkMode { get; set; } = WorkMode.FullProcess;
    }

    // 日志配置类
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
        // 基础配置
        public string IP { get; set; } = "192.168.1.88";
        public int Port { get; set; } = 502;
        public byte Station { get; set; } = 1;

        public string ScanTriggerAddress { get; set; } = "D500";        // PLC通知扫码
        public string TighteningTriggerAddress { get; set; } = "D501";  // PLC通知读取拧紧数据
        public string ScanResultAddress { get; set; } = "D520";         // 上位机反馈扫码结果 (1=OK, 2=NG)
        public string TighteningResultAddress { get; set; } = "D521";   // 上位机反馈拧紧结果 (1=成功, 2=超时)
        public string HeartbeatAddress { get; set; } = "D530";          // 心跳信号（持续写1）

        // 旧的字段（保留以兼容旧代码，但标记为过时）
        [Obsolete("请使用 TighteningTriggerAddress")]
        public string TriggerAddress { get; set; } = "D501";

        [Obsolete("请使用 TighteningResultAddress")]
        public string ScanCompleteAddress { get; set; } = "D521";

        [Obsolete("已废弃，新协议不使用M寄存器")]
        public string StartSignalAddress { get; set; } = "M100";

        [Obsolete("已废弃，新协议不使用M寄存器")]
        public string ConfirmSignalAddress { get; set; } = "M101";
    }


    public class ScannerConfig
    {
        public string IP { get; set; } = "192.168.1.74";
        public int Port { get; set; } = 6666;
        public int TimeoutSeconds { get; set; } = 10;
        public int BufferSize { get; set; } = 2048;
    }

    public class TighteningAxisConfig
    {
        public string IP { get; set; } = "192.168.1.76";
        public int Port { get; set; } = 502;
        public byte Station { get; set; } = 1;
        public int TimeoutSeconds { get; set; } = 10;

        // 拧紧轴特定配置
        public int StatusPollingIntervalMs { get; set; } = 2000; //从1000ms增加到2000ms减少轮询频率
        public int MaxOperationTimeoutSeconds { get; set; } = 1200; // 最大操作超时时间

        /// <summary>
        /// 启用错误恢复机制
        /// </summary>
        public bool EnableErrorRecovery { get; set; } = true;
        
        /// <summary>
        /// 最大连续错误次数，超过此数量将停止轮询
        /// </summary>
        public int MaxConsecutiveErrors { get; set; } = 3;
        
        /// <summary>
        /// 错误恢复延迟（毫秒），连续错误后的等待时间
        /// </summary>
        public int ErrorRecoveryDelayMs { get; set; } = 5000;
        
        /// <summary>
        /// 连接验证超时时间（毫秒）
        /// </summary>
        public int ConnectionValidationTimeoutMs { get; set; } = 3000;
        
        /// <summary>
        /// 是否在初始化时验证设备可用性（只有验证成功才启动轮询）
        /// </summary>
        public bool ValidateDeviceOnInit { get; set; } = true;
        
        /// <summary>
        /// 设备断开时是否自动停止轮询
        /// </summary>
        public bool AutoStopPollingOnDisconnect { get; set; } = true;
        
        /// <summary>
        /// 重连尝试间隔（秒）
        /// </summary>
        public int ReconnectIntervalSeconds { get; set; } = 30;

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

        // 角度相关
        public int RealtimeAngle { get; set; } = 5098;     // 实时角度
        
        public int TestRegister { get; set; } = 5000;      // 用于连接测试，使用紧固模式地址作为测试
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

    /// <summary>
    /// 工作模式枚举
    /// </summary>
    public enum WorkMode
    {
        /// <summary>
        /// 完整流程：接收前3道工序数据 + 工序4合并上传
        /// </summary>
        FullProcess = 0,
        /// <summary>
        /// 独立模式：仅执行工序4并独立上传（忽略前端数据）
        /// </summary>
        Independent = 1
    }
}
