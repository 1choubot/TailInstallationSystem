using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem.View
{
    public partial class SystemMonitorControl : UserControl
    {
        private CommunicationManager commManager;
        private TailInstallationController controller;

        public SystemMonitorControl(TailInstallationController controller, CommunicationManager commManager)
        {
            InitializeComponent();
            this.controller = controller;
            this.commManager = commManager;

            LogManager.OnLogWritten += OnLogMessage;

            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnSettings.Click += btnSettings_Click;
            btnEmergencyStop.Click += btnEmergencyStop_Click;
            btnClearLog.Click += btnClearLog_Click; 
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
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
            var lines = logTextBox.Lines;
            if (lines.Length > 1000)
            {
                var newLines = new string[500];
                Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                logTextBox.Lines = newLines;
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // statusLabel.Text = "正在启动系统..."; // 如有状态栏控件
                UpdateProgress(10);
                await controller.StartSystem();
                // statusLabel.Text = "系统运行中";
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                UpdateProgress(100);
                UpdateConnectionStatus();
                LogManager.LogInfo("系统启动成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // statusLabel.Text = "启动失败";
                UpdateProgress(0);
                LogManager.LogError($"系统启动失败: {ex.Message}");
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                // statusLabel.Text = "正在停止系统...";
                UpdateProgress(50);
                await controller.StopSystem();
                // statusLabel.Text = "系统已停止";
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
                // statusLabel.Text = "紧急停止";
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
            plcIndicator.BackColor = statusColor;
            plcStatusLabel.Text = statusText;
            plcStatusLabel.ForeColor = statusColor;
            scannerIndicator.BackColor = statusColor;
            scannerStatusLabel.Text = statusText;
            scannerStatusLabel.ForeColor = statusColor;
            screwIndicator.BackColor = statusColor;
            screwStatusLabel.Text = statusText;
            screwStatusLabel.ForeColor = statusColor;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogManager.OnLogWritten -= OnLogMessage;
            }
            base.Dispose(disposing);
        }
    }
}
