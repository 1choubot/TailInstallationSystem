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
                        // 自动处理配置文件升级
                        EnsureSystemSettingsExists(config);
                        _currentConfig = config;
                        LogManager.LogInfo("配置文件加载成功");
                    }
                    else
                    {
                        _currentConfig = CreateDefaultConfig();
                        SaveConfig(_currentConfig);
                        LogManager.LogInfo("配置文件内容为空，已创建默认配置");
                    }
                }
                else
                {
                    _currentConfig = CreateDefaultConfig();
                    SaveConfig(_currentConfig);
                    LogManager.LogInfo("创建默认配置文件");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载配置文件失败: {ex.Message}");
                _currentConfig = CreateDefaultConfig();

                // 尝试保存默认配置
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

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
                _currentConfig = config;

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
        }

        private static CommunicationConfig CreateDefaultConfig()
        {
            return new CommunicationConfig
            {
                PLC = new PLCConfig(),
                Scanner = new ScannerConfig(),
                ScrewDriver = new ScrewDriverConfig(),
                PC = new PCConfig(),
                Server = new ServerConfig(),
                System = new SystemSettings()
            };
        }
    }
}
