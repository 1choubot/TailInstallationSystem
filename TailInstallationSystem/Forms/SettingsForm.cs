using AntdUI;
using System;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    public partial class SettingsForm : Window
    {
        private int currentTabIndex = 0;

        public SettingsForm()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // 初始化下拉框
            logLevelComboBox.Items.Clear();
            logLevelComboBox.Items.Add("Debug");
            logLevelComboBox.Items.Add("Info");
            logLevelComboBox.Items.Add("Warning");
            logLevelComboBox.Items.Add("Error");
            logLevelComboBox.Text = "Info";

            // 设置默认值
            webSocketTextBox.Text = "ws://192.168.1.100:9001";

            // 默认显示基本设置面板
            ShowPanel(0);
        }

        private void ShowPanel(int index)
        {
            currentTabIndex = index;
            basicPanel.Visible = (index == 0);
            networkPanel.Visible = (index == 1);
        }

        // 如果Tabs控件有不同的事件，我们先用按钮来切换
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
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SaveSettings()
        {
            // 获取设置值
            bool autoStart = autoStartCheckBox.Checked;
            string logLevel = logLevelComboBox.Text;
            string webSocketUrl = webSocketTextBox.Text;

            // 这里实现保存逻辑
            // 例如保存到配置文件或注册表
        }
    }
}
