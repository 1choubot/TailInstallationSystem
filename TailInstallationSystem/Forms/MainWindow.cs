using AntdUI;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TailInstallationSystem
{
    public partial class MainWindow : AntdUI.Window
    {
        private CommunicationManager commManager;
        private TailInstallationController controller;
        private UserControl currentUserControl;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeCommunication();
            InitializeLogging();
        }

        private void InitializeEvents()
        {
            // 菜单按钮事件已在设计器中绑定
            LogManager.OnLogWritten += OnLogMessage;
        }

        private void InitializeCommunication()
        {
            commManager = new CommunicationManager();
            controller = new TailInstallationController(commManager);
        }

        private void InitializeLogging()
        {
            // 初始化日志显示
            LogManager.LogInfo("系统启动完成");
        }

        private void OnLogMessage(LogManager.LogLevel level, string timestamp, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<LogManager.LogLevel, string, string>(OnLogMessage), level, timestamp, message);
                return;
            }

            var logLine = $"[{timestamp}] [{level}] {message}";
            logTextBox.Text += logLine + Environment.NewLine;

            // 自动滚动到底部
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();

            // 限制日志行数，避免内存溢出
            var lines = logTextBox.Lines;
            if (lines.Length > 1000)
            {
                var newLines = new string[500];
                Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                logTextBox.Lines = newLines;
            }
        }

        // 菜单按钮事件处理
        private void btnSystemMonitor_Click(object sender, EventArgs e)
        {
            ShowSystemMonitor();
            UpdateMenuButtonState(btnSystemMonitor);
        }

        private void btnCommSettings_Click(object sender, EventArgs e)
        {
            ShowCommunicationSettings();
            UpdateMenuButtonState(btnCommSettings);
        }

        private void btnDataView_Click(object sender, EventArgs e)
        {
            ShowDataView();
            UpdateMenuButtonState(btnDataView);
        }

        private void btnUserManage_Click(object sender, EventArgs e)
        {
            ShowUserManagement();
            UpdateMenuButtonState(btnUserManage);
        }

        private void btnSystemLog_Click(object sender, EventArgs e)
        {
            ShowSystemLog();
            UpdateMenuButtonState(btnSystemLog);
        }

        private void UpdateMenuButtonState(AntdUI.Button activeButton)
        {
            // 重置所有按钮状态
            var buttons = new[] { btnSystemMonitor, btnCommSettings, btnDataView, btnUserManage, btnSystemLog };
            foreach (var btn in buttons)
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(64, 64, 64);
                btn.Type = TTypeMini.Default;
            }

            // 设置当前按钮为激活状态
            activeButton.BackColor = Color.FromArgb(24, 144, 255);
            activeButton.ForeColor = Color.White;
            activeButton.Type = TTypeMini.Primary;
        }

        private void ShowSystemMonitor()
        {
            // 显示系统监控界面（默认界面）
            // 这里已经是系统监控界面，无需切换
        }

        private void ShowCommunicationSettings()
        {
            // 显示通讯设置界面
            var commSettingsControl = new CommunicationSettingsControl();
            SwitchUserControl(commSettingsControl);
        }

        private void ShowDataView()
        {
            // 显示数据查看界面
            var dataViewControl = new DataViewControl();
            SwitchUserControl(dataViewControl);
        }

        private void ShowUserManagement()
        {
            // 显示用户管理界面
            var userManageControl = new UserManagementControl();
            SwitchUserControl(userManageControl);
        }

        private void ShowSystemLog()
        {
            // 显示系统日志界面
            var logControl = new SystemLogControl();
            SwitchUserControl(logControl);
        }

        private void SwitchUserControl(UserControl newControl)
        {
            // 移除当前控件
            if (currentUserControl != null)
            {
                contentPanel.Controls.Remove(currentUserControl);
                currentUserControl.Dispose();
            }

            // 添加新控件
            currentUserControl = newControl;
            newControl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(newControl);
            contentPanel.Controls.SetChildIndex(newControl, 0);
        }

        // 控制按钮事件处理
        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "正在启动系统...";
                UpdateProgress(10);

                await controller.StartSystem();

                statusLabel.Text = "系统运行中";
                btnStart.Enabled = false;
                btnStop.Enabled = true;

                UpdateProgress(100);
                UpdateConnectionStatus();

                LogManager.LogInfo("系统启动成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "启动失败";
                UpdateProgress(0);
                LogManager.LogError($"系统启动失败: {ex.Message}");
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "正在停止系统...";
                UpdateProgress(50);

                await controller.StopSystem();

                statusLabel.Text = "系统已停止";
                btnStart.Enabled = true;
                btnStop.Enabled = false;

                UpdateProgress(0);
                UpdateConnectionStatus(false);

                LogManager.LogInfo("系统已停止");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogManager.LogError($"系统停止失败: {ex.Message}");
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog(this);
        }

        private void btnEmergencyStop_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要紧急停止系统吗？这将立即中断所有操作！",
                "紧急停止确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                controller?.EmergencyStop();
                statusLabel.Text = "紧急停止";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                UpdateProgress(0);
                LogManager.LogWarning("系统紧急停止");
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            logTextBox.Clear();
            LogManager.LogInfo("日志已清空");
        }

        // 辅助方法
        private void UpdateProgress(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(UpdateProgress), value);
                return;
            }
            progressBar.Value = value;
        }

        private void UpdateConnectionStatus(bool connected = true)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(UpdateConnectionStatus), connected);
                return;
            }

            var statusColor = connected ? Color.FromArgb(82, 196, 26) : Color.FromArgb(255, 77, 79);
            var statusText = connected ? "已连接" : "未连接";

            // 更新各设备状态
            plcIndicator.BackColor = statusColor;
            plcStatusLabel.Text = statusText;
            plcStatusLabel.ForeColor = statusColor;

            scannerIndicator.BackColor = statusColor;
            scannerStatusLabel.Text = statusText;
            scannerStatusLabel.ForeColor = statusColor;

            screwIndicator.BackColor = statusColor;
            screwStatusLabel.Text = statusText;
            screwStatusLabel.ForeColor = statusColor;

            // PC通讯状态可能不同
            var pcColor = connected ? Color.FromArgb(250, 173, 20) : Color.FromArgb(255, 77, 79);
            var pcText = connected ? "等待数据" : "未连接";
            pcIndicator.BackColor = pcColor;
            pcStatusLabel.Text = pcText;
            pcStatusLabel.ForeColor = pcColor;
        }

        public void UpdateCurrentProduct(string barcode, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(UpdateCurrentProduct), barcode, status);
                return;
            }

            currentBarcodeLabel.Text = $"当前产品条码: {barcode}";
            currentStatusLabel.Text = $"状态: {status}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (controller != null)
            {
                controller.StopSystem();
            }
            base.OnFormClosing(e);
        }
    }
}