using System;

public class TighteningAxisData
{
    #region 基本属性

    /// <summary>
    /// 数据时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 状态码（地址5104）
    /// 11 = 合格
    /// 21~30 = 不合格（21=扭矩低, 22=扭矩高, 23=超时, 24=角度低, 25=角度高）
    /// </summary>
    public int StatusCode { get; set; }


    #endregion

    #region 扭矩相关数据

    /// <summary>
    /// 完成扭矩（地址5094）
    /// 运行中为实时扭矩，完成后为最终扭矩
    /// </summary>
    public float CompletedTorque { get; set; }

    /// <summary>
    /// 目标扭矩（地址5006）
    /// </summary>
    public float TargetTorque { get; set; }

    /// <summary>
    /// 判断下限扭矩（地址5002）
    /// </summary>
    public float LowerLimitTorque { get; set; }

    /// <summary>
    /// 判断上限扭矩（地址5004）
    /// </summary>
    public float UpperLimitTorque { get; set; }

    #endregion

    #region 角度相关数据

    /// <summary>
    /// 完成角度（地址5102）
    /// 运行中为实时角度，完成后为最终角度
    /// </summary>
    public float CompletedAngle { get; set; }

    /// <summary>
    /// 目标角度（地址5032）
    /// </summary>
    public float TargetAngle { get; set; }

    /// <summary>
    /// 判断下限角度（地址5042）
    /// </summary>
    public float LowerLimitAngle { get; set; }

    /// <summary>
    /// 判断上限角度（地址5044）
    /// </summary>
    public float UpperLimitAngle { get; set; }

    #endregion

    #region 统计数据

    /// <summary>
    /// 合格数记录（地址5090）
    /// </summary>
    public int QualifiedCount { get; set; }

    /// <summary>
    /// 反馈速度（地址5100）
    /// 运行中的实时速度
    /// </summary>
    public float FeedbackSpeed { get; set; }

    #endregion

    #region 计算属性


    /// <summary>
    /// 拧紧操作是否完成
    /// 基于状态码判断：10或11~30表示已完成
    /// </summary>
    public bool IsOperationCompleted
    {
        get
        {
            return StatusCode == 11 || (StatusCode >= 21 && StatusCode <= 30);
        }
    }

    /// <summary>
    /// 是否正在运行
    /// 状态码为1表示运行中
    /// </summary>
    public bool IsRunning => StatusCode == 1;

    /// <summary>
    /// 拧紧结果是否合格
    /// 完全信任设备的状态判断（5104）
    /// </summary>
    public bool IsQualified
    {
        get
        {
            if (!IsOperationCompleted)
                return false;
            return StatusCode == 11;
        }
    }

    /// <summary>
    /// 不合格原因描述
    /// 基于5104状态码
    /// </summary>
    public string QualityResult
    {
        get
        {
            if (!IsOperationCompleted)
                return "未完成";
            switch (StatusCode)
            {
                case 11: 
                    return "合格";
                case 21:
                    return "扭矩过低";
                case 22:
                    return "扭矩过高";
                case 23:
                    return "运行超时";
                case 24:
                    return "角度过低";
                case 25:
                    return "角度过高";
                default:
                    if (StatusCode >= 21 && StatusCode <= 30)
                        return $"不合格(代码{StatusCode})";
                    return $"未知状态({StatusCode})";
            }
        }
    }

    /// <summary>
    /// 数据是否有效
    /// </summary>
    public bool IsDataValid
    {
        get
        {
            if (!IsOperationCompleted)
                return false;
            if (CompletedTorque < 0 || CompletedTorque > 100)
                return false;
            return true;
        }
    }

    /// <summary>
    /// 扭矩达成率（实际扭矩/目标扭矩 * 100%）
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
        var description = $"状态: {GetStatusDisplayName()}, " +
                         $"扭矩: {CompletedTorque:F2}Nm, " +
                         $"角度: {Math.Abs(CompletedAngle):F1}°";

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
        if (StatusCode == 0)
            return "空闲";
        if (StatusCode == 1)
            return "运行中";
        if (StatusCode == 11)
            return "合格";
        if (StatusCode >= 21 && StatusCode <= 30)
            return "不合格";
        if (StatusCode == 500 || StatusCode == 1000)
            return "执行命令中";

        return $"未知({StatusCode})";
    }

    /// <summary>
    /// 转换为JSON字符串（用于数据上传）
    /// </summary>
    public string ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            statusCode = StatusCode,
            statusName = GetStatusDisplayName(),
            completedTorque = CompletedTorque,
            completedAngle = CompletedAngle,
            targetTorque = TargetTorque,
            targetAngle = TargetAngle,
            lowerLimitTorque = LowerLimitTorque,
            upperLimitTorque = UpperLimitTorque,
            lowerLimitAngle = LowerLimitAngle,
            upperLimitAngle = UpperLimitAngle,
            qualifiedCount = QualifiedCount,
            feedbackSpeed = FeedbackSpeed,
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
            StatusCode = isQualified ? 10 : 21,
            CompletedTorque = isQualified ? 29.1f : 27.5f,
            CompletedAngle = isQualified ? 720.5f : 680.2f,
            TargetTorque = 29.0f,
            TargetAngle = 720.0f,
            LowerLimitTorque = 28.0f,
            UpperLimitTorque = 30.0f,
            LowerLimitAngle = 650.0f,
            UpperLimitAngle = 800.0f,
            QualifiedCount = isQualified ? 1 : 0,
            FeedbackSpeed = 0f
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
