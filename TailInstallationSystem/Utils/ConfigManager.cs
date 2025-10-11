using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using TailInstallationSystem.Models;

namespace TailInstallationSystem.Utils
{
    public static class ConfigManager
    {
        private static readonly string ConfigFile = Path.Combine(
            Application.StartupPath, "Config", "communication.json");

        private static CommunicationConfig _currentConfig;

        // 配置变更事件（可选订阅）
        public static event Action<CommunicationConfig> OnConfigChanged;
        private static string _lastAppliedLogLevel = null;

        static ConfigManager()
        {
            // 确保配置目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
        }

        public static CommunicationConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    var config = JsonConvert.DeserializeObject<CommunicationConfig>(json);
                    if (config != null)
                    {
                        EnsureSystemSettingsExists(config);

                        // 验证配置
                        var validation = ConfigValidator.ValidateConfig(config);
                        if (!validation.IsValid)
                        {
                            LogManager.LogError("配置文件验证失败:");
                            foreach (var error in validation.Errors)
                            {
                                LogManager.LogError($"  - {error}");
                            }

                            // 显示验证错误但不阻止程序启动
                            MessageBox.Show(
                                $"配置文件存在错误:\n\n{validation.GetSummary()}\n" +
                                "程序将使用默认值继续运行，请检查配置！",
                                "配置验证警告",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                        else if (validation.Warnings.Count > 0)
                        {
                            foreach (var warning in validation.Warnings)
                            {
                                LogManager.LogWarning($"配置警告: {warning}");
                            }
                        }
                        _currentConfig = config;
                        
                        // 应用日志设置
                        ApplyLoggingSettings(config);
                        
                        LogManager.LogInfo("配置文件加载成功");
                    }
                    else
                    {
                        LogManager.LogWarning("配置文件内容为空，创建默认配置");
                        _currentConfig = CreateDefaultConfig();
                        SaveConfig(_currentConfig);
                    }
                }
                else
                {
                    LogManager.LogInfo("配置文件不存在，创建默认配置");
                    _currentConfig = CreateDefaultConfig();
                    SaveConfig(_currentConfig);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载配置文件失败: {ex.Message}");
                _currentConfig = CreateDefaultConfig();
                try
                {
                    SaveConfig(_currentConfig);
                }
                catch
                {
                    LogManager.LogError("无法保存默认配置，程序将使用内存中的默认值");
                }
            }
            return _currentConfig;
        }

        public static void SaveConfig(CommunicationConfig config)
        {
            try
            {
                EnsureSystemSettingsExists(config);
                // 保存前验证配置
                var validation = ConfigValidator.ValidateConfig(config);
                if (!validation.IsValid)
                {
                    var errorMessage = $"配置验证失败，无法保存:\n\n{validation.GetSummary()}";
                    LogManager.LogError("配置保存失败: " + errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                if (validation.Warnings.Count > 0)
                {
                    LogManager.LogWarning("配置保存时发现警告:");
                    foreach (var warning in validation.Warnings)
                    {
                        LogManager.LogWarning($"  - {warning}");
                    }
                }
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
                _currentConfig = config;
                
                // 应用日志设置
                ApplyLoggingSettings(config);
                
                LogManager.LogInfo("配置文件保存成功");
                try
                {
                    OnConfigChanged?.Invoke(config);
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"配置变更事件处理异常: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存配置文件失败: {ex.Message}");
                throw;
            }
        }

        // 应用日志设置到LogManager
        private static void ApplyLoggingSettings(CommunicationConfig config)
        {
            try
            {
                if (config?.System?.LogLevel != null)
                {
                    // 只在首次设置或级别改变时记录
                    if (_lastAppliedLogLevel == null)
                    {
                        LogManager.SetLogLevel(config.System.LogLevel);
                        LogManager.LogInfo($"日志级别初始化为: {config.System.LogLevel}");
                        _lastAppliedLogLevel = config.System.LogLevel;
                    }
                    else if (_lastAppliedLogLevel != config.System.LogLevel)
                    {
                        LogManager.SetLogLevel(config.System.LogLevel);
                        LogManager.LogInfo($"日志级别已更改: {_lastAppliedLogLevel} → {config.System.LogLevel}");
                        _lastAppliedLogLevel = config.System.LogLevel;
                    }
                    // 级别未改变时不输出日志
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"应用日志设置失败: {ex.Message}");
            }
        }

        // 验证当前配置的公共方法
        public static ValidationResult ValidateCurrentConfig()
        {
            var config = GetCurrentConfig();
            return ConfigValidator.ValidateConfig(config);
        }

        public static CommunicationConfig GetCurrentConfig()
        {
            if (_currentConfig == null)
            {
                return LoadConfig();
            }

            // 确保返回的配置有完整的System设置
            EnsureSystemSettingsExists(_currentConfig);
            return _currentConfig;
        }

        public static SystemSettings GetSystemSettings()
        {
            var config = GetCurrentConfig();
            return config.System;
        }

        public static void UpdateSystemSettings(SystemSettings systemSettings)
        {
            var config = GetCurrentConfig();
            config.System = systemSettings ?? new SystemSettings();
            SaveConfig(config);
            LogManager.LogInfo("系统设置已更新");
        }

        public static void UpdateSystemSetting<T>(string settingName, T value)
        {
            try
            {
                var config = GetCurrentConfig();
                var systemType = typeof(SystemSettings);
                var property = systemType.GetProperty(settingName);

                if (property != null && property.CanWrite)
                {
                    property.SetValue(config.System, value);
                    SaveConfig(config);
                    LogManager.LogInfo($"系统设置已更新: {settingName} = {value}");
                }
                else
                {
                    LogManager.LogWarning($"未找到系统设置属性: {settingName}");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新系统设置失败: {ex.Message}");
                throw;
            }
        }

        private static void EnsureSystemSettingsExists(CommunicationConfig config)
        {
            if (config.System == null)
            {
                config.System = new SystemSettings();
                LogManager.LogInfo("自动添加了系统设置部分到配置中");
            }
            
            if (config.System.Logging == null)
            {
                config.System.Logging = new LoggingSettings();
                LogManager.LogInfo("自动添加了日志配置部分到配置中");
            }
        }

        // 创建包含完整日志配置的默认配置
        private static CommunicationConfig CreateDefaultConfig()
        {
            return new CommunicationConfig
            {
                PLC = new PLCConfig(),
                Scanner = new ScannerConfig(),
                TighteningAxis = new TighteningAxisConfig(), 
                PC = new PCConfig(),
                Server = new ServerConfig(),
                System = new SystemSettings() // 这里会自动使用CommunicationConfig.cs中定义的默认值
            };
        }

        // 强制重新生成配置文件（保留现有设置）
        public static void RegenerateConfigWithNewFields()
        {
            try
            {
                LogManager.LogInfo("开始更新配置文件以包含新字段...");
                
                var config = GetCurrentConfig();
                EnsureSystemSettingsExists(config); // 这会添加缺失的Logging字段
                SaveConfig(config); // 保存更新后的配置
                
                LogManager.LogInfo("配置文件已更新，包含所有新字段");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新配置文件失败: {ex.Message}");
                throw;
            }
        }
    }
}
