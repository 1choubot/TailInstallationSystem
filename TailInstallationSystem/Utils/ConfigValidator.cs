using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TailInstallationSystem.Models;

namespace TailInstallationSystem.Utils
{
    public static class ConfigValidator
    {
        public static ValidationResult ValidateConfig(CommunicationConfig config)
        {
            var result = new ValidationResult();

            if (config == null)
            {
                result.AddError("配置对象为空");
                return result;
            }

            // 验证PLC配置
            ValidatePLCConfig(config.PLC, result);

            // 验证扫码枪配置
            ValidateScannerConfig(config.Scanner, result);

            // 验证拧紧轴配置
            ValidateTighteningAxisConfig(config.TighteningAxis, result);

            // 验证服务器配置
            ValidateServerConfig(config.Server, result);

            // 验证系统设置
            ValidateSystemSettings(config.System, result);

            return result;
        }

        private static void ValidatePLCConfig(PLCConfig plc, ValidationResult result)
        {
            if (plc == null)
            {
                result.AddError("PLC配置为空");
                return;
            }

            // 验证IP地址
            if (string.IsNullOrWhiteSpace(plc.IP))
            {
                result.AddError("PLC IP地址不能为空");
            }
            else if (!IPAddress.TryParse(plc.IP, out _))
            {
                result.AddError($"PLC IP地址格式无效: {plc.IP}");
            }

            // 验证端口
            if (plc.Port < 1 || plc.Port > 65535)
            {
                result.AddError($"PLC端口号无效: {plc.Port}，必须在1-65535范围内");
            }

            // 验证站号
            if (plc.Station < 1 || plc.Station > 247)
            {
                result.AddError($"PLC站号无效: {plc.Station}，必须在1-247范围内");
            }

            if (string.IsNullOrWhiteSpace(plc.ScanTriggerAddress))
            {
                result.AddError("PLC扫码触发地址不能为空");
            }
            else if (!IsValidModbusAddress(plc.ScanTriggerAddress))
            {
                result.AddError($"PLC扫码触发地址格式无效: {plc.ScanTriggerAddress}");
            }

            if (string.IsNullOrWhiteSpace(plc.TighteningTriggerAddress))
            {
                result.AddError("PLC拧紧触发地址不能为空");
            }
            else if (!IsValidModbusAddress(plc.TighteningTriggerAddress))
            {
                result.AddError($"PLC拧紧触发地址格式无效: {plc.TighteningTriggerAddress}");
            }

            if (string.IsNullOrWhiteSpace(plc.ScanResultAddress))
            {
                result.AddError("PLC扫码结果反馈地址不能为空");
            }
            else if (!IsValidModbusAddress(plc.ScanResultAddress))
            {
                result.AddError($"PLC扫码结果反馈地址格式无效: {plc.ScanResultAddress}");
            }

            if (string.IsNullOrWhiteSpace(plc.TighteningResultAddress))
            {
                result.AddError("PLC拧紧结果反馈地址不能为空");
            }
            else if (!IsValidModbusAddress(plc.TighteningResultAddress))
            {
                result.AddError($"PLC拧紧结果反馈地址格式无效: {plc.TighteningResultAddress}");
            }

            if (string.IsNullOrWhiteSpace(plc.HeartbeatAddress))
            {
                result.AddError("PLC心跳地址不能为空");
            }
            else if (!IsValidModbusAddress(plc.HeartbeatAddress))
            {
                result.AddError($"PLC心跳地址格式无效: {plc.HeartbeatAddress}");
            }

        }

        private static void ValidateScannerConfig(ScannerConfig scanner, ValidationResult result)
        {
            if (scanner == null)
            {
                result.AddError("扫码枪配置为空");
                return;
            }

            ValidateIPAndPort(scanner.IP, scanner.Port, "扫码枪", result);

            if (scanner.TimeoutSeconds < 1 || scanner.TimeoutSeconds > 300)
            {
                result.AddError($"扫码枪超时时间无效: {scanner.TimeoutSeconds}秒，建议在1-300秒范围内");
            }

            if (scanner.BufferSize < 512 || scanner.BufferSize > 65536)
            {
                result.AddError($"扫码枪缓冲区大小无效: {scanner.BufferSize}字节，建议在512-65536字节范围内");
            }
        }

        // 拧紧轴配置验证方法
        private static void ValidateTighteningAxisConfig(TighteningAxisConfig tighteningAxis, ValidationResult result)
        {
            if (tighteningAxis == null)
            {
                result.AddError("拧紧轴配置为空");
                return;
            }

            // 验证基本网络配置
            ValidateIPAndPort(tighteningAxis.IP, tighteningAxis.Port, "拧紧轴", result);

            // 验证Modbus站号
            if (tighteningAxis.Station < 1 || tighteningAxis.Station > 247)
            {
                result.AddError($"拧紧轴Modbus站号无效: {tighteningAxis.Station}，必须在1-247范围内");
            }

            // 验证超时配置
            if (tighteningAxis.TimeoutSeconds < 1 || tighteningAxis.TimeoutSeconds > 300)
            {
                result.AddError($"拧紧轴超时时间无效: {tighteningAxis.TimeoutSeconds}秒，建议在1-300秒范围内");
            }

            // 验证轮询间隔
            if (tighteningAxis.StatusPollingIntervalMs < 100 || tighteningAxis.StatusPollingIntervalMs > 5000)
            {
                result.AddError($"拧紧轴状态轮询间隔无效: {tighteningAxis.StatusPollingIntervalMs}毫秒，建议在100-5000毫秒范围内");
            }

            // 验证最大操作超时时间
            if (tighteningAxis.MaxOperationTimeoutSeconds < 1 || tighteningAxis.MaxOperationTimeoutSeconds > 300)
            {
                result.AddError($"拧紧轴最大操作超时时间无效: {tighteningAxis.MaxOperationTimeoutSeconds}秒，建议在1-300秒范围内");
            }
            else if (tighteningAxis.MaxOperationTimeoutSeconds < 3)
            {
                result.AddWarning($"拧紧轴最大操作超时时间较短: {tighteningAxis.MaxOperationTimeoutSeconds}秒，" +
                                 "D501触发时数据应已就绪，建议设置为3秒处理通讯抖动");
            }
            else if (tighteningAxis.MaxOperationTimeoutSeconds > 10)
            {
                result.AddWarning($"拧紧轴最大操作超时时间较长: {tighteningAxis.MaxOperationTimeoutSeconds}秒，" +
                                 "在新协议下(D501=拧紧已完成)，建议缩短为3-5秒");
            }

            // 验证扭矩范围
            if (tighteningAxis.MinTorque < 0 || tighteningAxis.MaxTorque <= tighteningAxis.MinTorque)
            {
                result.AddError($"拧紧轴扭矩范围无效: {tighteningAxis.MinTorque}-{tighteningAxis.MaxTorque}Nm");
            }

            // 验证寄存器地址配置
            ValidateModbusRegisterAddresses(tighteningAxis.Registers, result);

            // 特殊验证：检查拧紧轴的默认IP是否正确
            if (tighteningAxis.IP == "192.168.0.102" && tighteningAxis.Port != 502)
            {
                result.AddWarning("拧紧轴使用标准IP地址但端口不是标准的502，请确认配置是否正确");
            }
        }

        // 验证Modbus寄存器地址配置
        private static void ValidateModbusRegisterAddresses(ModbusRegisterAddresses registers, ValidationResult result)
        {
            if (registers == null)
            {
                result.AddError("拧紧轴寄存器地址配置为空");
                return;
            }

            var addressesToCheck = new Dictionary<string, int>
            {
                ["状态码"] = registers.StatusCode,              
                ["完成扭矩"] = registers.CompletedTorque,
                ["完成角度"] = registers.CompletedAngle,       
                ["目标扭矩"] = registers.TargetTorque,
                ["下限扭矩"] = registers.LowerLimitTorque,
                ["上限扭矩"] = registers.UpperLimitTorque,
                ["目标角度"] = registers.TargetAngle,          
                ["下限角度"] = registers.LowerLimitAngle,      
                ["上限角度"] = registers.UpperLimitAngle,      
                ["程序号"] = registers.ProgramNumber,          
                ["合格数量"] = registers.QualifiedCount,
                ["紧固模式"] = registers.TighteningMode,
                ["反馈速度"] = registers.FeedbackSpeed         
            };

            foreach (var address in addressesToCheck)
            {
                if (address.Value < 5000 || address.Value > 5110)  
                {
                    result.AddWarning($"拧紧轴{address.Key}地址({address.Value})超出标准范围(5000-5110)，请确认是否正确");
                }
            }

            var criticalAddresses = new Dictionary<string, int>
            {
                ["状态码"] = registers.StatusCode,        
                ["完成扭矩"] = registers.CompletedTorque,
                ["完成角度"] = registers.CompletedAngle     
            };

            foreach (var address in criticalAddresses)
            {
                if (address.Value % 2 != 0)
                {
                    result.AddError($"拧紧轴{address.Key}地址({address.Value})必须为偶数地址，因为32位数据占用2个寄存器");
                }
            }
        }


        private static void ValidateServerConfig(ServerConfig server, ValidationResult result)
        {
            if (server == null)
            {
                result.AddError("服务器配置为空");
                return;
            }

            // 验证WebSocket URL
            if (string.IsNullOrWhiteSpace(server.WebSocketUrl))
            {
                result.AddError("WebSocket服务器地址不能为空");
            }
            else
            {
                try
                {
                    var uri = new Uri(server.WebSocketUrl);
                    if (!uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase) &&
                        !uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddError($"WebSocket地址必须以ws://或wss://开头: {server.WebSocketUrl}");
                    }

                    if (uri.Port < 1 || uri.Port > 65535)
                    {
                        result.AddError($"WebSocket端口号无效: {uri.Port}");
                    }
                }
                catch (UriFormatException ex)
                {
                    result.AddError($"WebSocket地址格式无效: {server.WebSocketUrl}, 错误: {ex.Message}");
                }
            }

            // 验证重试配置
            if (server.RetryIntervalMinutes < 1 || server.RetryIntervalMinutes > 1440)
            {
                result.AddError($"重试间隔无效: {server.RetryIntervalMinutes}分钟，建议在1-1440分钟范围内");
            }

            if (server.MaxRetryAttempts < 1 || server.MaxRetryAttempts > 100)
            {
                result.AddError($"最大重试次数无效: {server.MaxRetryAttempts}，建议在1-100范围内");
            }
        }

        private static void ValidateSystemSettings(SystemSettings system, ValidationResult result)
        {
            if (system == null)
            {
                result.AddError("系统设置为空");
                return;
            }

            // 验证日志级别
            var validLogLevels = new[] { "Debug", "Info", "Warning", "Error" };
            if (string.IsNullOrWhiteSpace(system.LogLevel) ||
                Array.IndexOf(validLogLevels, system.LogLevel) < 0)
            {
                result.AddError($"日志级别无效: {system.LogLevel}，有效值: {string.Join(", ", validLogLevels)}");
            }

            // 验证重连间隔
            if (system.ReconnectIntervalSeconds < 5 || system.ReconnectIntervalSeconds > 3600)
            {
                result.AddError($"重连间隔无效: {system.ReconnectIntervalSeconds}秒，建议在5-3600秒范围内");
            }

            // 验证数据保留天数
            if (system.DataRetentionDays < 1 || system.DataRetentionDays > 3650)
            {
                result.AddError($"数据保留天数无效: {system.DataRetentionDays}天，建议在1-3650天范围内");
            }

            // 验证连接超时
            if (system.ConnectionTimeoutSeconds < 1 || system.ConnectionTimeoutSeconds > 300)
            {
                result.AddError($"连接超时时间无效: {system.ConnectionTimeoutSeconds}秒，建议在1-300秒范围内");
            }
        }

        private static void ValidateIPAndPort(string ip, int port, string deviceName, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                result.AddError($"{deviceName} IP地址不能为空");
            }
            else if (!IPAddress.TryParse(ip, out _))
            {
                result.AddError($"{deviceName} IP地址格式无效: {ip}");
            }

            if (port < 1 || port > 65535)
            {
                result.AddError($"{deviceName}端口号无效: {port}，必须在1-65535范围内");
            }
        }

        private static bool IsValidModbusAddress(string address)
        {
            // 简单的Modbus地址验证，支持M、D、X、Y等常见类型
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // 匹配M100, D200, X001, Y002等格式
            var pattern = @"^[MDXY]\d+$";
            return Regex.IsMatch(address.ToUpper(), pattern);
        }
    }

    public class ValidationResult
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors.AsReadOnly();
        public IReadOnlyList<string> Warnings => warnings.AsReadOnly();

        public void AddError(string error)
        {
            errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            warnings.Add(warning);
        }

        public string GetSummary()
        {
            var summary = new StringBuilder();

            if (errors.Count > 0)
            {
                summary.AppendLine($"发现 {errors.Count} 个配置错误:");
                foreach (var error in errors)
                {
                    summary.AppendLine($"  • {error}");
                }
            }

            if (warnings.Count > 0)
            {
                if (summary.Length > 0) summary.AppendLine();
                summary.AppendLine($"发现 {warnings.Count} 个配置警告:");
                foreach (var warning in warnings)
                {
                    summary.AppendLine($"  • {warning}");
                }
            }

            if (IsValid && warnings.Count == 0)
            {
                summary.AppendLine("配置验证通过，无错误或警告。");
            }

            return summary.ToString();
        }
    }
}
