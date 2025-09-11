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
        private Timer statusCheckTimer;

        // Device status tracking
        private enum DeviceStatus
        {
            Connected,      // 已连接 - 绿色
            Disconnected,   // 未连接 - 红色  
            Waiting,        // 等待数据 - 橙色
            Working         // 工作中 - 蓝色
        }

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

            // Subscribe to communication events
            if (commManager != null)
            {
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnDataReceived += OnDataReceived;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnScrewDataReceived += OnScrewDataReceived;
            }

            // Subscribe to controller business events
            if (controller != null)
            {
                controller.OnProcessStatusChanged += OnProcessStatusChanged;
                controller.OnCurrentProductChanged += OnCurrentProductChanged;
            }

            // Initialize status check timer
            InitializeStatusTimer();
            
            // Initialize all devices to disconnected state and set default messages
            InitializeDefaultState();
        }

        /// <summary>
        /// Initialize the default UI state
        /// </summary>
        private void InitializeDefaultState()
        {
            UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
            UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
            UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
            UpdateDeviceStatus("PC", DeviceStatus.Disconnected);
            
            // Set default product information
            currentBarcodeLabel.Text = "当前产品条码: 等待扫描...";
            currentStatusLabel.Text = "状态: 系统未启动";
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
                
                // Start status monitoring
                StartStatusMonitoring();
                
                // Update system status
                currentStatusLabel.Text = "状态: 系统运行中";
                
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
                
                // Stop status monitoring
                StopStatusMonitoring();
                
                await controller.StopSystem();
                // statusLabel.Text = "系统已停止";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                UpdateProgress(0);
                
                // Update all devices to disconnected
                UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
                UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
                UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
                UpdateDeviceStatus("PC", DeviceStatus.Disconnected);
                
                // Reset system status
                currentBarcodeLabel.Text = "当前产品条码: 等待扫描...";
                currentStatusLabel.Text = "状态: 系统已停止";
                
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
                
                // Stop status monitoring immediately
                StopStatusMonitoring();
                
                // statusLabel.Text = "紧急停止";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                UpdateProgress(0);
                
                // Update all devices to disconnected
                UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
                UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
                UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
                UpdateDeviceStatus("PC", DeviceStatus.Disconnected);
                
                // Reset system status 
                currentBarcodeLabel.Text = "当前产品条码: 等待扫描...";
                currentStatusLabel.Text = "状态: 紧急停止";
                
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

        /// <summary>
        /// Update individual device status with specific state
        /// </summary>
        private void UpdateDeviceStatus(string deviceName, DeviceStatus status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, DeviceStatus>(UpdateDeviceStatus), deviceName, status);
                return;
            }

            if (string.IsNullOrEmpty(deviceName))
            {
                LogManager.LogWarning("UpdateDeviceStatus called with null or empty device name");
                return;
            }

            Color statusColor;
            string statusText;

            switch (status)
            {
                case DeviceStatus.Connected:
                    statusColor = Color.FromArgb(82, 196, 26);  // Green
                    statusText = "已连接";
                    break;
                case DeviceStatus.Disconnected:
                    statusColor = Color.FromArgb(255, 77, 79);   // Red
                    statusText = "未连接";
                    break;
                case DeviceStatus.Waiting:
                    statusColor = Color.FromArgb(250, 173, 20);  // Orange
                    statusText = "等待数据";
                    break;
                case DeviceStatus.Working:
                    statusColor = Color.FromArgb(24, 144, 255);  // Blue
                    statusText = "工作中";
                    break;
                default:
                    statusColor = Color.FromArgb(128, 128, 128); // Gray
                    statusText = "未知状态";
                    break;
            }

            try
            {
                switch (deviceName.ToUpper())
                {
                    case "PLC":
                        plcIndicator.BackColor = statusColor;
                        plcStatusLabel.Text = statusText;
                        plcStatusLabel.ForeColor = statusColor;
                        break;
                    case "SCANNER":
                        scannerIndicator.BackColor = statusColor;
                        scannerStatusLabel.Text = statusText;
                        scannerStatusLabel.ForeColor = statusColor;
                        break;
                    case "SCREWDRIVER":
                        screwIndicator.BackColor = statusColor;
                        screwStatusLabel.Text = statusText;
                        screwStatusLabel.ForeColor = statusColor;
                        break;
                    case "PC":
                        pcIndicator.BackColor = statusColor;
                        pcStatusLabel.Text = statusText;
                        pcStatusLabel.ForeColor = statusColor;
                        break;
                    default:
                        LogManager.LogWarning($"Unknown device name: {deviceName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error updating device status for {deviceName}: {ex.Message}");
            }
        }

        #region Communication Event Handlers

        /// <summary>
        /// Handle device connection status changes
        /// </summary>
        private void OnDeviceConnectionChanged(string deviceName, bool isConnected)
        {
            var status = isConnected ? DeviceStatus.Connected : DeviceStatus.Disconnected;
            UpdateDeviceStatus(deviceName, status);
            
            string statusText = isConnected ? "已连接" : "断开连接";
            LogManager.LogInfo($"设备状态变化: {deviceName} {statusText}");
        }

        /// <summary>
        /// Handle PC data received
        /// </summary>
        private void OnDataReceived(string data)
        {
            UpdateDeviceStatus("PC", DeviceStatus.Working);
            LogManager.LogInfo("PC接收到工序数据");
            
            // Set PC back to waiting status after a short delay
            Task.Delay(2000).ContinueWith(_ => {
                try
                {
                    UpdateDeviceStatus("PC", DeviceStatus.Waiting);
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"PC状态更新异常: {ex.Message}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Handle barcode scanned
        /// </summary>
        private void OnBarcodeScanned(string barcode)
        {
            UpdateDeviceStatus("Scanner", DeviceStatus.Working);
            LogManager.LogInfo($"扫码枪扫描到条码: {barcode}");
            
            // Set scanner back to connected status after scanning
            Task.Delay(1000).ContinueWith(_ => {
                try
                {
                    UpdateDeviceStatus("Scanner", DeviceStatus.Connected);
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"Scanner状态更新异常: {ex.Message}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Handle screw driver data received
        /// </summary>
        private void OnScrewDataReceived(string screwData)
        {
            UpdateDeviceStatus("ScrewDriver", DeviceStatus.Working);
            LogManager.LogInfo("螺丝机执行安装操作");
            
            // Set screw driver back to connected status after operation
            Task.Delay(3000).ContinueWith(_ => {
                try
                {
                    UpdateDeviceStatus("ScrewDriver", DeviceStatus.Connected);
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"ScrewDriver状态更新异常: {ex.Message}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion

        #region Business Process Event Handlers

        /// <summary>
        /// Handle business process status changes from controller
        /// </summary>
        private void OnProcessStatusChanged(string barcode, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(OnProcessStatusChanged), barcode, status);
                return;
            }

            currentStatusLabel.Text = $"状态: {status}";
            LogManager.LogInfo($"业务流程状态: {status}");
        }

        /// <summary>
        /// Handle current product changes from controller
        /// </summary>
        private void OnCurrentProductChanged(string barcode, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(OnCurrentProductChanged), barcode, status);
                return;
            }

            if (!string.IsNullOrEmpty(barcode))
            {
                currentBarcodeLabel.Text = $"当前产品条码: {barcode}";
            }
            currentStatusLabel.Text = $"状态: {status}";
        }

        #endregion

        #region Status Monitoring Timer

        /// <summary>
        /// Initialize the status checking timer
        /// </summary>
        private void InitializeStatusTimer()
        {
            statusCheckTimer = new Timer();
            statusCheckTimer.Interval = 5000; // Check every 5 seconds
            statusCheckTimer.Tick += OnStatusTimerTick;
        }

        /// <summary>
        /// Timer tick event for periodic status checking
        /// </summary>
        private void OnStatusTimerTick(object sender, EventArgs e)
        {
            if (commManager == null || !btnStop.Enabled) return;

            try
            {
                // Check device connections periodically
                // Note: This is a basic implementation. In a real scenario, you might want to
                // implement more sophisticated connection health checks
                
                // The actual connection status should come from CommunicationManager events
                // This timer serves as a backup check and can trigger reconnection attempts
                LogManager.LogDebug("执行定期设备状态检查");
                
                // You could add additional health checks here, such as:
                // - Ping network devices
                // - Check socket connection state
                // - Validate communication heartbeat
            }
            catch (Exception ex)
            {
                LogManager.LogError($"状态检查异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Start the status monitoring timer
        /// </summary>
        private void StartStatusMonitoring()
        {
            statusCheckTimer?.Start();
            LogManager.LogInfo("设备状态监控已启动");
        }

        /// <summary>
        /// Stop the status monitoring timer  
        /// </summary>
        private void StopStatusMonitoring()
        {
            statusCheckTimer?.Stop();
            LogManager.LogInfo("设备状态监控已停止");
        }

        #endregion

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

        /// <summary>
        /// Public method for external device status updates (called by MainWindow)
        /// </summary>
        public void UpdateExternalDeviceStatus(string deviceName, bool isConnected)
        {
            var status = isConnected ? DeviceStatus.Connected : DeviceStatus.Disconnected;
            UpdateDeviceStatus(deviceName, status);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LogManager.OnLogWritten -= OnLogMessage;
                
                // Unsubscribe from communication events
                if (commManager != null)
                {
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnDataReceived -= OnDataReceived;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnScrewDataReceived -= OnScrewDataReceived;
                }
                
                // Unsubscribe from controller business events
                if (controller != null)
                {
                    controller.OnProcessStatusChanged -= OnProcessStatusChanged;
                    controller.OnCurrentProductChanged -= OnCurrentProductChanged;
                }
                
                // Stop and dispose timer
                statusCheckTimer?.Stop();
                statusCheckTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
