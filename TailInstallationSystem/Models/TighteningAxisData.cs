using System;
using TailInstallationSystem;
using TailInstallationSystem.Models;

public class TighteningAxisData
{
    private static int _lastControlCommand = 0;
    private static bool _hasReportedCompletion = false;

    #region 基本属性

    /// <summary>
    /// 数据时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 控制命令字 (地址5102)
    /// 100=启动命令, 0=运行完成, 300=停止命令
    /// </summary>
    public ushort ControlCommand { get; set; }  

    /// <summary>
    /// 运行状态代码 (地址5100)
    /// </summary>
    public ushort RunningStatusCode { get; set; } 

    /// <summary>
    /// 运行状态枚举
    /// </summary>
    public TighteningStatus Status { get; set; }

    /// <summary>
    /// 错误代码 (地址5096)
    /// 正常时为0，不为0时故障
    /// </summary>
    public int ErrorCode { get; set; }  

    #endregion

    #region 扭矩相关数据

    /// <summary>
    /// 完成扭矩 (地址5092) - 拧紧完成后的最终扭矩值
    /// </summary>
    public float CompletedTorque { get; set; }

    /// <summary>
    /// 实时扭矩 (地址5094) - 拧紧过程中的实时扭矩
    /// </summary>
    public float RealtimeTorque { get; set; }

    /// <summary>
    /// 目标扭矩 (地址5006)
    /// </summary>
    public float TargetTorque { get; set; }

    /// <summary>
    /// 判断下限扭矩 (地址5002)
    /// </summary>
    public float LowerLimitTorque { get; set; }

    /// <summary>
    /// 判断上限扭矩 (地址5004)
    /// </summary>
    public float UpperLimitTorque { get; set; }

    /// <summary>
    /// 实时角度 (地址5098)
    /// </summary>
    public float RealtimeAngle { get; set; }

    #endregion

    #region 统计数据

    /// <summary>
    /// 合格数记录 (地址5088)
    /// </summary>
    public int QualifiedCount { get; set; }

    #endregion

    #region 计算属性

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasError => ErrorCode != 0;

    /// <summary>
    /// 拧紧操作是否完成
    /// 判断标准：控制命令字从100变为0，且运行状态不是运行中
    /// </summary>
    public bool IsOperationCompleted
    {
        get
        {
            // 记录控制命令变化
            bool commandChanged = _lastControlCommand == 100 && ControlCommand == 0;
            if (ControlCommand != _lastControlCommand)
            {
                LogManager.LogDebug($"控制命令字变化: {_lastControlCommand} → {ControlCommand}");
                _lastControlCommand = ControlCommand;
            }

            // 方案1：检测从100变为0（最可靠）
            if (commandChanged)
            {
                LogManager.LogInfo("检测到拧紧操作完成（控制命令字100→0）");
                return true;
            }

            // 方案2：控制命令为0且有明确的结果状态
            if (ControlCommand == 0 && RunningStatusCode >= 10)
            {
                return true;
            }

            // 方案3：控制命令为0且有完成扭矩
            if (ControlCommand == 0 && CompletedTorque > 0.01f)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => ControlCommand == 100 || Status == TighteningStatus.Running;

    /// <summary>
    /// 拧紧结果是否合格
    /// </summary>
    public bool IsQualified
    {
        get
        {
            // 只有在操作完成时才判断合格性
            if (!IsOperationCompleted)
                return false;

            // 根据运行状态判断
            return Status == TighteningStatus.Qualified;
        }
    }

    /// <summary>
    /// 不合格原因描述
    /// </summary>
    public string QualityResult
    {
        get
        {
            if (!IsOperationCompleted)
                return "未完成";

            switch (Status)
            {
                case TighteningStatus.Qualified:
                    return "合格";
                case TighteningStatus.TorqueTooLow:
                    return "扭矩过低";
                case TighteningStatus.TorqueTooHigh:
                    return "扭矩过高";
                case TighteningStatus.TimeoutError:
                    return "超时错误";
                case TighteningStatus.AngleTooLow:
                    return "角度过低";
                case TighteningStatus.AngleTooHigh:
                    return "角度过高";
                case TighteningStatus.Error:
                    return $"系统错误(代码:{ErrorCode})";
                default:
                    return $"未知状态({RunningStatusCode})";
            }
        }
    }

    /// <summary>
    /// 扭矩是否在范围内
    /// </summary>
    public bool IsTorqueInRange
    {
        get
        {
            if (CompletedTorque <= 0 || LowerLimitTorque <= 0 || UpperLimitTorque <= 0)
                return false;

            return CompletedTorque >= LowerLimitTorque && CompletedTorque <= UpperLimitTorque;
        }
    }

    /// <summary>
    /// 扭矩达成率 (实际扭矩/目标扭矩 * 100%)
    /// </summary>
    public double TorqueAchievementRate
    {
        get
        {
            if (TargetTorque <= 0 || CompletedTorque <= 0)
                return 0;

            return (CompletedTorque / TargetTorque) * 100;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取状态描述
    /// </summary>
    public string GetStatusDescription()
    {
        var description = $"状态: {GetStatusDisplayName()}, 扭矩: {CompletedTorque:F2}Nm";

        if (HasError)
        {
            description += $", 错误代码: 0x{ErrorCode:X4}";
        }

        if (IsOperationCompleted)
        {
            description += $", 结果: {QualityResult}";
        }

        return description;
    }

    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    public string GetStatusDisplayName()
    {
        switch (Status)
        {
            case TighteningStatus.Idle:
                return "空闲";
            case TighteningStatus.Running:
                return "运行中";
            case TighteningStatus.Qualified:
                return "合格";
            case TighteningStatus.TorqueTooLow:
                return "扭矩过低";
            case TighteningStatus.TorqueTooHigh:
                return "扭矩过高";
            case TighteningStatus.TimeoutError:
                return "超时错误";
            case TighteningStatus.AngleTooLow:
                return "角度过低";
            case TighteningStatus.AngleTooHigh:
                return "角度过高";
            case TighteningStatus.Error:
                return "错误";
            default:
                return $"未知({RunningStatusCode})";
        }
    }

    /// <summary>
    /// 转换为JSON字符串（用于数据上传）
    /// </summary>
    public string ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            controlCommand = ControlCommand,
            runningStatus = RunningStatusCode,
            statusName = GetStatusDisplayName(),
            errorCode = ErrorCode,
            completedTorque = CompletedTorque,
            realtimeTorque = RealtimeTorque,
            targetTorque = TargetTorque,
            lowerLimitTorque = LowerLimitTorque,
            upperLimitTorque = UpperLimitTorque,
            qualifiedCount = QualifiedCount,
            isCompleted = IsOperationCompleted,
            isQualified = IsQualified,
            qualityResult = QualityResult,
            torqueAchievementRate = Math.Round(TorqueAchievementRate, 2)
        }, Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// 创建测试数据（用于调试）
    /// </summary>
    public static TighteningAxisData CreateTestData(bool isQualified = true)
    {
        return new TighteningAxisData
        {
            Timestamp = DateTime.Now,
            ControlCommand = 0, // 已完成
            RunningStatusCode = isQualified ? (ushort)10 : (ushort)21,
            Status = isQualified ? TighteningStatus.Qualified : TighteningStatus.TorqueTooLow,
            ErrorCode = 0,
            CompletedTorque = isQualified ? 29.1f : 27.5f,
            RealtimeTorque = 0f,
            TargetTorque = 29.0f,
            LowerLimitTorque = 28.0f,
            UpperLimitTorque = 30.0f,
            QualifiedCount = isQualified ? 1 : 0
        };
    }

    #endregion

    #region 重写方法

    public override string ToString()
    {
        return GetStatusDescription();
    }

    #endregion
}

