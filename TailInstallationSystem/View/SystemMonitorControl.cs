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
                commManager.OnTighteningDataReceived += OnTighteningDataReceived; 
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
            UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected); 
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
                UpdateProgress(10);
                await controller.StartSystem();
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                UpdateProgress(100);

                StartStatusMonitoring();

                currentStatusLabel.Text = "状态: 系统运行中";

                LogManager.LogInfo("系统启动成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected); 
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
                UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected); 
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
                    case "TIGHTENINGAXIS": 
                        tighteningAxisIndicator.BackColor = statusColor;
                        tighteningAxisStatusLabel.Text = statusText;
                        tighteningAxisStatusLabel.ForeColor = statusColor;
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

        private void OnTighteningDataReceived(TighteningAxisData tighteningData)
        {
            if (CheckDisposed()) return;
            try
            {
                SafeInvoke(() =>
                {
                    if (tighteningData.IsRunning)
                    {
                        UpdateDeviceStatus("TighteningAxis", DeviceStatus.Working);
                        LogManager.LogInfo($"拧紧轴运行中 - 实时扭矩: {tighteningData.RealtimeTorque:F2}Nm");
                    }
                    else if (tighteningData.IsOperationCompleted)
                    {
                        UpdateDeviceStatus("TighteningAxis", DeviceStatus.Connected);
                        LogManager.LogInfo($"拧紧操作完成 - 完成扭矩: {tighteningData.CompletedTorque:F2}Nm, 结果: {tighteningData.QualityResult}");
                    }
                    else
                    {
                        UpdateDeviceStatus("TighteningAxis", DeviceStatus.Connected);
                    }

                    // 如果有错误，显示错误状态
                    if (tighteningData.HasError)
                    {
                        UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected);
                        LogManager.LogError($"拧紧轴错误: 错误代码{tighteningData.ErrorCode}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧轴数据接收事件处理异常: {ex.Message}");
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
            statusCheckTimer.Interval = 5000;
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
                        commManager.OnTighteningDataReceived -= OnTighteningDataReceived; 
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