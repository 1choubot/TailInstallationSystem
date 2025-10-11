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
            LoadWorkModeSettings();
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

        /// <summary>
        /// 加载工作模式设置
        /// </summary>
        private void LoadWorkModeSettings()
        {
            try
            {
                var config = ConfigManager.GetCurrentConfig();
                var currentMode = config.System.CurrentWorkMode;

                // 设置开关状态（不触发事件）
                workModeSwitch.CheckedChanged -= workModeSwitch_CheckedChanged;
                workModeSwitch.Checked = (currentMode == Models.WorkMode.Independent);
                workModeSwitch.CheckedChanged += workModeSwitch_CheckedChanged;

                // 更新标签显示
                UpdateWorkModeLabel(currentMode);

                // 更新控制器模式
                if (controller != null)
                {
                    controller.UpdateWorkMode(currentMode);
                }

                LogManager.LogInfo($"工作模式已加载: {GetWorkModeDisplayName(currentMode)}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载工作模式配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 工作模式开关变更事件
        /// </summary>
        private void workModeSwitch_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
        {
            try
            {
                var newMode = e.Value ? Models.WorkMode.Independent : Models.WorkMode.FullProcess;

                // 显示确认对话框
                var message = e.Value
                    ? "您正在切换到【独立模式】：\n\n" +
                      "• 将忽略前端发送的工序1-3数据\n" +
                      "• 仅执行扫码→拧紧→上传工序4\n" +
                      "• 数据库中工序1-3列为空\n\n" +
                      "是否确认切换？"
                    : "您正在切换到【完整流程模式】：\n\n" +
                      "• 接收并缓存前端发送的工序1-3数据\n" +
                      "• 执行完整的4道工序流程\n" +
                      "• 数据合并后上传\n\n" +
                      "是否确认切换？";

                var result = MessageBox.Show(
                    message,
                    "工作模式切换确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 保存配置
                    var config = ConfigManager.GetCurrentConfig();
                    config.System.CurrentWorkMode = newMode;
                    ConfigManager.SaveConfig(config);

                    // 更新控制器
                    if (controller != null)
                    {
                        controller.UpdateWorkMode(newMode);
                    }

                    // 更新UI显示
                    UpdateWorkModeLabel(newMode);

                    LogManager.LogInfo($"工作模式已切换: {GetWorkModeDisplayName(newMode)}");

                    // 显示成功提示
                    AntdUI.Message.success(this.FindForm(), "工作模式切换成功！", autoClose: 2);
                }
                else
                {
                    // 用户取消，恢复开关状态（不触发事件）
                    workModeSwitch.CheckedChanged -= workModeSwitch_CheckedChanged;
                    workModeSwitch.Checked = !e.Value;
                    workModeSwitch.CheckedChanged += workModeSwitch_CheckedChanged;

                    LogManager.LogInfo("用户取消工作模式切换");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"切换工作模式失败: {ex.Message}");
                AntdUI.Message.error(this.FindForm(), $"切换失败: {ex.Message}", autoClose: 3);
            }
        }

        /// <summary>
        /// 更新工作模式标签显示
        /// </summary>
        private void UpdateWorkModeLabel(Models.WorkMode mode)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Models.WorkMode>(UpdateWorkModeLabel), mode);
                return;
            }

            switch (mode)
            {
                case Models.WorkMode.FullProcess:
                    workModeLabel.Text = "工作模式：完整流程";
                    workModeLabel.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
                    break;
                case Models.WorkMode.Independent:
                    workModeLabel.Text = "工作模式：独立模式";
                    workModeLabel.ForeColor = System.Drawing.Color.FromArgb(250, 173, 20); // 橙色
                    break;
            }
        }

        /// <summary>
        /// 获取工作模式显示名称
        /// </summary>
        private string GetWorkModeDisplayName(Models.WorkMode mode)
        {
            switch (mode)
            {
                case Models.WorkMode.FullProcess:
                    return "完整流程模式";
                case Models.WorkMode.Independent:
                    return "独立模式（仅工序4）";
                default:
                    return "未知模式";
            }
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

                // 先初始化设备连接
                if (commManager != null)
                {
                    LogManager.LogInfo("开始初始化设备连接...");
                    bool connectResult = await commManager.InitializeConnections();
                    if (!connectResult)
                    {
                        MessageBox.Show("设备连接初始化失败，请检查设备状态", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UpdateProgress(0);
                        return;
                    }
                }

                UpdateProgress(30); // 可选：调整进度显示更细致
                if (commManager != null && commManager.IsPLCConnected)
                {
                    try
                    {
                        await commManager.StartHeartbeat();
                        LogManager.LogInfo("系统启动完成，心跳信号已启动");
                    }
                    catch (Exception heartbeatEx)
                    {
                        LogManager.LogWarning($"心跳启动失败: {heartbeatEx.Message}，系统将继续启动");
                        MessageBox.Show("心跳信号启动失败，但系统将继续运行", "警告", 
                             MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                UpdateProgress(50);

                // 然后启动系统
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
                try
                {
                    commManager?.StopHeartbeat();
                    LogManager.LogInfo("系统启动失败，已停止心跳信号");
                }
                catch (Exception stopEx)
                {
                    LogManager.LogWarning($"停止心跳信号失败: {stopEx.Message}");
                }
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

                // 添加null检查
                if (controller != null)
                {
                    await controller.StopSystem();
                }
                else
                {
                    LogManager.LogWarning("控制器对象为空，跳过停止操作");
                }

                if (commManager != null)
                {
                    try
                    {
                        commManager.StopHeartbeat();
                        LogManager.LogInfo("系统已停止，心跳信号已停止");
                    }
                    catch (Exception heartbeatEx)
                    {
                        LogManager.LogWarning($"停止心跳信号失败: {heartbeatEx.Message}");
                    }
                }

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
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
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

                try
                {
                    commManager?.StopHeartbeat();
                    LogManager.LogWarning("紧急停止：心跳信号已停止");
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"紧急停止心跳失败: {ex.Message}");
                }

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
                        //LogManager.LogInfo($"拧紧轴运行中 - 实时扭矩: {tighteningData.RealtimeTorque:F2}Nm");
                    }
                    else if (tighteningData.IsOperationCompleted)
                    {
                        UpdateDeviceStatus("TighteningAxis", DeviceStatus.Connected);
                        //LogManager.LogInfo($"拧紧操作完成 - 完成扭矩: {tighteningData.CompletedTorque:F2}Nm, 结果: {tighteningData.QualityResult}");
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
            if (status.Contains("成功") || status.Contains("失败") ||
                 status.Contains("完成") || status.Contains("合格") ||
                 status.Contains("不合格"))
            {
                LogManager.LogInfo($"状态更新 | {status}");
            }
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
                // 检测PLC连接状态
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (commManager.IsPLCConnected)
                        {
                            // 尝试读取心跳寄存器验证连接
                            var heartbeatValue = await commManager.ReadPLCDRegister(commManager.GetCurrentConfig().PLC.HeartbeatAddress);
                            if (!heartbeatValue.HasValue)
                            {
                                // 读取失败，判定为断开
                                SafeInvoke(() => UpdateDeviceStatus("PLC", DeviceStatus.Disconnected));
                            }
                        }
                    }
                    catch
                    {
                        SafeInvoke(() => UpdateDeviceStatus("PLC", DeviceStatus.Disconnected));
                    }
                });

                // 检测拧紧轴连接状态
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (commManager.IsTighteningAxisConnected)
                        {
                            // 尝试读取测试寄存器验证连接
                            var testData = await commManager.ReadTighteningAxisData();
                            if (testData == null)
                            {
                                SafeInvoke(() => UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected));
                            }
                        }
                    }
                    catch
                    {
                        SafeInvoke(() => UpdateDeviceStatus("TighteningAxis", DeviceStatus.Disconnected));
                    }
                });
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