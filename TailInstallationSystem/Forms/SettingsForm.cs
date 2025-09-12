using AntdUI;
using System;
using System.Windows.Forms;
using TailInstallationSystem.Utils;
using TailInstallationSystem.Models;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace TailInstallationSystem
{
    public partial class SettingsForm : AntdUI.Window
    {
        private int currentTabIndex = 0;
        private CommunicationConfig _workingConfig; // 工作副本，避免直接修改全局配置

        public SettingsForm()
        {
            InitializeComponent();
            InitializeSettings();
            LoadAllSettings(); 
        }

        private void InitializeSettings()
        {
            logLevelComboBox.Items.Clear();
            logLevelComboBox.Items.Add("Debug");
            logLevelComboBox.Items.Add("Info");
            logLevelComboBox.Items.Add("Warning");
            logLevelComboBox.Items.Add("Error");

            ShowPanel(0);
        }

        private void LoadAllSettings()
        {
            try
            {
                // 获取当前配置并创建工作副本
                var currentConfig = ConfigManager.GetCurrentConfig();
                var json = JsonConvert.SerializeObject(currentConfig);
                _workingConfig = JsonConvert.DeserializeObject<CommunicationConfig>(json);

                // 加载基本设置
                LoadBasicSettings();

                // 加载网络设置
                LoadNetworkSettings();

                LogManager.LogInfo("设置界面已加载完整配置");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载设置失败: {ex.Message}");
                MessageBox.Show($"加载设置失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // 创建默认配置作为后备
                _workingConfig = new CommunicationConfig();
            }
        }

        private void LoadBasicSettings()
        {
            var system = _workingConfig.System;

            // 加载系统设置
            autoStartCheckBox.Checked = system.AutoStart;
            logLevelComboBox.Text = system.LogLevel;
        }

        private void LoadNetworkSettings()
        {
            // 加载WebSocket设置
            webSocketTextBox.Text = _workingConfig.Server.WebSocketUrl;
        }

        private void ShowPanel(int index)
        {
            currentTabIndex = index;
            basicPanel.Visible = (index == 0);
            networkPanel.Visible = (index == 1);

            basicTabButton.Type = (index == 0) ? TTypeMini.Primary : TTypeMini.Default;
            networkTabButton.Type = (index == 1) ? TTypeMini.Primary : TTypeMini.Default;
        }

        private void basicTabButton_Click(object sender, EventArgs e)
        {
            ShowPanel(0);
        }

        private void networkTabButton_Click(object sender, EventArgs e)
        {
            ShowPanel(1);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SaveSettings()
        {
            try
            {
                // 验证输入
                if (!ValidateInputs())
                    return;

                // 保存基本设置
                SaveBasicSettings();

                // 保存网络设置
                SaveNetworkSettings();

                // 一次性保存所有配置
                ConfigManager.SaveConfig(_workingConfig);

                // 立即应用一些设置
                ApplyImmediateSettings();

                LogManager.LogInfo("所有设置已保存并应用");

                MessageBox.Show(
                    "设置已保存成功！\n\n" +
                    "• 日志级别等设置立即生效\n" +
                    "• 开机自启等设置需重启系统后生效\n" +
                    "• 网络设置可能需要重新连接设备",
                    "保存成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存设置失败: {ex.Message}");
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveBasicSettings()
        {
            var system = _workingConfig.System;

            system.AutoStart = autoStartCheckBox.Checked;
            system.LogLevel = logLevelComboBox.Text;
        }

        private void SaveNetworkSettings()
        {
            _workingConfig.Server.WebSocketUrl = webSocketTextBox.Text.Trim();
        }

        private bool ValidateInputs()
        {
            // 验证WebSocket URL
            var wsUrl = webSocketTextBox.Text.Trim();
            if (string.IsNullOrEmpty(wsUrl))
            {
                MessageBox.Show("WebSocket服务器地址不能为空！", "输入错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPanel(1); // 切换到网络设置
                webSocketTextBox.Focus();
                return false;
            }

            if (!wsUrl.StartsWith("ws://") && !wsUrl.StartsWith("wss://"))
            {
                MessageBox.Show("WebSocket地址格式不正确！\n应以 ws:// 或 wss:// 开头", "输入错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPanel(1); // 切换到网络设置
                webSocketTextBox.Focus();
                webSocketTextBox.SelectAll();
                return false;
            }

            // 验证日志级别
            if (string.IsNullOrEmpty(logLevelComboBox.Text))
            {
                MessageBox.Show("请选择日志级别！", "输入错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPanel(0); // 切换到基本设置
                logLevelComboBox.Focus();
                return false;
            }

            return true;
        }

        private void ApplyImmediateSettings()
        {
            try
            {
                var system = _workingConfig.System;

                // 立即应用日志级别
                LogManager.SetLogLevel(system.LogLevel);

                // 设置开机自启动
                SetAutoStart(system.AutoStart);

                LogManager.LogInfo($"设置已应用 - 日志级别: {system.LogLevel}, 自启动: {system.AutoStart}");
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"部分设置应用失败: {ex.Message}");
            }
        }

        // 设置开机自启动
        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    var appName = "TailInstallationSystem";

                    if (enable)
                    {
                        var exePath = Application.ExecutablePath;
                        key?.SetValue(appName, $"\"{exePath}\"");
                        LogManager.LogInfo("已设置开机自启动");
                    }
                    else
                    {
                        key?.DeleteValue(appName, false);
                        LogManager.LogInfo("已取消开机自启动");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"设置开机自启动失败: {ex.Message}");
                throw new Exception($"设置开机自启动失败: {ex.Message}");
            }
        }

    }
}
