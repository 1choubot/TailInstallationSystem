using Newtonsoft.Json;
using System;

namespace TailInstallationSystem.Models
{
    public class CommunicationConfig
    {
        public PLCConfig PLC { get; set; } = new PLCConfig();
        public ScannerConfig Scanner { get; set; } = new ScannerConfig();
        public TighteningAxisConfig TighteningAxis { get; set; } = new TighteningAxisConfig();

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
        public int StatusPollingIntervalMs { get; set; } = 2000; // 状态轮询间隔
        public int MaxOperationTimeoutSeconds { get; set; } = 3; // 最大操作超时时间（D501触发后读取数据的超时）

        /// <summary>
        /// 读取已完成数据的最大重试次数
        /// </summary>
        public int ReadCompletedDataMaxRetries { get; set; } = 3;

        /// <summary>
        /// 读取重试间隔（毫秒）
        /// </summary>
        public int ReadRetryDelayMs { get; set; } = 200;

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

        // 扭矩范围配置（用于数据验证）
        public double MinTorque { get; set; } = 0.1;
        public double MaxTorque { get; set; } = 100.0;

        // 关键寄存器地址
        public ModbusRegisterAddresses Registers { get; set; } = new ModbusRegisterAddresses();
    }

    // 拧紧轴Modbus寄存器地址配置
    public class ModbusRegisterAddresses
    {
        // ==================== 配置参数（写入） ====================
        public int TighteningMode { get; set; } = 5000;           // 紧固模式
        public int LowerLimitTorque { get; set; } = 5002;         // 判断下限扭矩
        public int UpperLimitTorque { get; set; } = 5004;         // 判断上限扭矩
        public int TargetTorque { get; set; } = 5006;             // 目标扭矩
        public int TargetAngle { get; set; } = 5032;              // 目标角度
        public int TargetSpeed { get; set; } = 5066;              // 目标速度
        public int LowerLimitAngle { get; set; } = 5042;          // 判断下限角度
        public int UpperLimitAngle { get; set; } = 5044;          // 判断上限角度

        // ==================== 反馈数据（读取） ====================

        // 核心结果数据（最关键）
        public int StatusCode { get; set; } = 5104;               // 状态码（11=合格，21~30=不合格）
        public int CompletedTorque { get; set; } = 5094;          // 完成扭矩（最终扭矩值）
        public int CompletedAngle { get; set; } = 5102;           // 完成角度（最终角度值）

        // 统计数据
        public int ProgramNumber { get; set; } = 5088;            // 程序号
        public int QualifiedCount { get; set; } = 5090;           // 合格数量
        public int CycleWorkpiece { get; set; } = 5086;           // 循环工艺

        // 实时运行数据
        public int FeedbackSpeed { get; set; } = 5100;            // 反馈速度（运行速度）


        // ==================== 测试用 ====================
        public int TestRegister { get; set; } = 5000;

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
