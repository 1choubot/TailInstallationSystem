using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem.View
{
    public partial class SystemMonitorControl : UserControl
    {
        private CommunicationManager commManager;
        private TailInstallationController controller;
        private System.Windows.Forms.Timer statusCheckTimer;
        private CancellationTokenSource cancellationTokenSource;

        // 状态锁，保证线程安全
        private readonly object disposeLock = new object();
        private volatile bool isDisposed = false;
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
            this.cancellationTokenSource = new CancellationTokenSource();

            LogManager.OnLogWritten += OnLogMessage;

            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnSettings.Click += btnSettings_Click;
            btnEmergencyStop.Click += btnEmergencyStop_Click;
            btnClearLog.Click += btnClearLog_Click;

            if (commManager != null)
            {
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnDataReceived += OnDataReceived;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnScrewDataReceived += OnScrewDataReceived;
            }

            if (controller != null)
            {
                controller.OnProcessStatusChanged += OnProcessStatusChanged;
                controller.OnCurrentProductChanged += OnCurrentProductChanged;
            }

            InitializeStatusTimer();

            InitializeDefaultState();
        }

        
        private void InitializeDefaultState()
        {
            UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
            UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
            UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
            UpdateDeviceStatus("PC", DeviceStatus.Disconnected);

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
                // statusLabel.Text = "正在启动系统..."; 
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
                UpdateProgress(50);

                StopStatusMonitoring();

                await controller.StopSystem();

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                UpdateProgress(0);

                // 重置设备状态...
                UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
                UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
                UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
                UpdateDeviceStatus("PC", DeviceStatus.Disconnected);

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
                // 立即停止定时器！
                StopStatusMonitoring();

                controller?.EmergencyStop();

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                UpdateProgress(0);

                // 重置所有设备状态...
                UpdateDeviceStatus("PLC", DeviceStatus.Disconnected);
                UpdateDeviceStatus("Scanner", DeviceStatus.Disconnected);
                UpdateDeviceStatus("ScrewDriver", DeviceStatus.Disconnected);
                UpdateDeviceStatus("PC", DeviceStatus.Disconnected);

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

        
        private void OnDeviceConnectionChanged(string deviceName, bool isConnected)
        {
            var status = isConnected ? DeviceStatus.Connected : DeviceStatus.Disconnected;
            UpdateDeviceStatus(deviceName, status);

            string statusText = isConnected ? "已连接" : "断开连接";
            LogManager.LogInfo($"设备状态变化: {deviceName} {statusText}");
        }


        private void OnDataReceived(string data)
        {
            if (CheckDisposed()) return;
            try
            {
                SafeInvoke(() => UpdateDeviceStatus("PC", DeviceStatus.Working));
                LogManager.LogInfo("PC接收到工序数据");
                // 安全的异步状态重置
                _ = SafeDelayedAction(2000, () =>
                {
                    SafeInvoke(() => UpdateDeviceStatus("PC", DeviceStatus.Waiting));
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PC数据接收事件处理异常: {ex.Message}");
            }
        }



        private void OnBarcodeScanned(string barcode)
        {
            if (CheckDisposed()) return;
            try
            {
                SafeInvoke(() => UpdateDeviceStatus("Scanner", DeviceStatus.Working));
                LogManager.LogInfo($"扫码枪扫描到条码: {barcode}");
                _ = SafeDelayedAction(1000, () =>
                {
                    SafeInvoke(() => UpdateDeviceStatus("Scanner", DeviceStatus.Connected));
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError($"扫码枪事件处理异常: {ex.Message}");
            }
        }

        private void OnScrewDataReceived(string screwData)
        {
            if (CheckDisposed()) return;
            try
            {
                SafeInvoke(() => UpdateDeviceStatus("ScrewDriver", DeviceStatus.Working));
                LogManager.LogInfo("螺丝机执行安装操作");
                _ = SafeDelayedAction(3000, () =>
                {
                    SafeInvoke(() => UpdateDeviceStatus("ScrewDriver", DeviceStatus.Connected));
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError($"螺丝机事件处理异常: {ex.Message}");
            }
        }
        private void SafeInvoke(Action action)
        {
            if (CheckDisposed()) return;
            try
            {
                if (InvokeRequired)
                {
                    if (!CheckDisposed())
                    {
                        BeginInvoke(new Action(() =>
                        {
                            if (!CheckDisposed())
                            {
                                action?.Invoke();
                            }
                        }));
                    }
                }
                else
                {
                    action?.Invoke();
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放，忽略
            }
            catch (InvalidOperationException)
            {
                // 控件句柄无效，忽略
            }
            catch (Exception ex)
            {
                LogManager.LogError($"UI调用异常: {ex.Message}");
            }
        }
        private async Task SafeDelayedAction(int delayMs, Action action)
        {
            try
            {
                await Task.Delay(delayMs, cancellationTokenSource.Token);

                if (!CheckDisposed())
                {
                    SafeInvoke(action);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不记录错误
            }
            catch (Exception ex)
            {
                LogManager.LogError($"延迟执行异常: {ex.Message}");
            }
        }
        private bool CheckDisposed()
        {
            lock (disposeLock)
            {
                return isDisposed || IsDisposed || !IsHandleCreated;
            }
        }

        #endregion

        #region Business Process Event Handlers

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

        
        private void InitializeStatusTimer()
        {
            statusCheckTimer = new System.Windows.Forms.Timer();
            statusCheckTimer.Interval = 5000; // Check every 5 seconds
            statusCheckTimer.Tick += OnStatusTimerTick;
        }

        private void OnStatusTimerTick(object sender, EventArgs e)
        {
            if (commManager == null || !btnStop.Enabled || statusCheckTimer?.Enabled != true)
            {
                return;
            }

            try
            {
                 //LogManager.LogDebug("执行定期设备状态检查");

                // 实际的健康检查逻辑...
            }
            catch (Exception ex)
            {
                LogManager.LogError($"状态检查异常: {ex.Message}");
            }
        }

        private void StartStatusMonitoring()
        {
            statusCheckTimer?.Start();
            LogManager.LogInfo("设备状态监控已启动");
        }

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


        public void UpdateExternalDeviceStatus(string deviceName, bool isConnected)
        {
            var status = isConnected ? DeviceStatus.Connected : DeviceStatus.Disconnected;
            UpdateDeviceStatus(deviceName, status);
        }

        protected override void Dispose(bool disposing)
        {
            lock (disposeLock)
            {
                if (!isDisposed && disposing)
                {
                    isDisposed = true;

                    // 取消所有异步任务
                    cancellationTokenSource?.Cancel();

                    // 取消订阅事件
                    LogManager.OnLogWritten -= OnLogMessage;
                    if (commManager != null)
                    {
                        commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                        commManager.OnDataReceived -= OnDataReceived;
                        commManager.OnBarcodeScanned -= OnBarcodeScanned;
                        commManager.OnScrewDataReceived -= OnScrewDataReceived;
                    }
                    if (controller != null)
                    {
                        controller.OnProcessStatusChanged -= OnProcessStatusChanged;
                        controller.OnCurrentProductChanged -= OnCurrentProductChanged;
                    }

                    statusCheckTimer?.Stop();
                    statusCheckTimer?.Dispose();

                    // 释放取消令牌
                    cancellationTokenSource?.Dispose();
                }
            }

            base.Dispose(disposing);
        }

    }
}