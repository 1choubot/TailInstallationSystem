using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using TailInstallationSystem.Models;

namespace TailInstallationSystem.Utils
{
    public static class ConfigManager
    {
        private static readonly string ConfigFile = Path.Combine(Application.StartupPath, "Config", "communication.json");
        private static CommunicationConfig _currentConfig;

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
                    _currentConfig = JsonConvert.DeserializeObject<CommunicationConfig>(json) ?? new CommunicationConfig();
                    LogManager.LogInfo("配置文件加载成功");
                }
                else
                {
                    _currentConfig = new CommunicationConfig();
                    SaveConfig(_currentConfig); // 创建默认配置文件
                    LogManager.LogInfo("创建默认配置文件");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载配置文件失败: {ex.Message}");
                _currentConfig = new CommunicationConfig();
            }

            return _currentConfig;
        }

        public static void SaveConfig(CommunicationConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
                _currentConfig = config;
                LogManager.LogInfo("配置文件保存成功");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存配置文件失败: {ex.Message}");
                throw;
            }
        }

        public static CommunicationConfig GetCurrentConfig()
        {
            return _currentConfig ?? LoadConfig();
        }
    }
}
