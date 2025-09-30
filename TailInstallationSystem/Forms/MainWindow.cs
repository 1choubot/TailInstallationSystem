using AntdUI;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public partial class MainWindow : AntdUI.Window
    {
        private CommunicationManager commManager;
        private TailInstallationController controller;
        private UserControl currentUserControl;
        private View.SystemMonitorControl systemMonitorControl;
        private View.SystemLogControl systemLogControl;
        private LicenseManager licenseManager;
        private bool isLicenseValid = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeLogging();
            licenseManager = new LicenseManager();
            licenseManager.CheckActive();
            isLicenseValid = licenseManager.ShowActive();

            if (isLicenseValid)
            {
                // 只创建通讯管理器，不初始化连接
                var config = ConfigManager.LoadConfig();
                commManager = new CommunicationManager(config);

                // 添加事件订阅
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnDataReceived += OnDataReceived;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnTighteningDataReceived += OnTighteningDataReceived;
                commManager.OnPLCTrigger += OnPLCTrigger;

                // 创建控制器
                controller = new TailInstallationController(commManager);

                ShowSystemMonitor();
                UpdateMenuButtonState(btnSystemMonitor);
            }
            else
            {
                this.Load += (s, e) => {
                    MessageBox.Show("授权验证失败，程序将退出", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                };
            }
        }



        private void InitializeEvents()
        {
            // 菜单按钮事件已在设计器中绑定
        }

        private async Task InitializeCommunication()
        {
            try
            {
                // 加载配置
                var config = ConfigManager.LoadConfig();

                // 释放旧的通讯管理器
                commManager?.Dispose();

                // 创建新的通讯管理器并传入配置
                commManager = new CommunicationManager(config);

                // 添加事件订阅：监听设备连接状态变化
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnDataReceived += OnDataReceived;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnTighteningDataReceived += OnTighteningDataReceived;
                commManager.OnPLCTrigger += OnPLCTrigger;

                // 【重要】初始化所有设备连接
                bool connectResult = await commManager.InitializeConnections();
                if (!connectResult)
                {
                    LogManager.LogError("设备连接初始化失败");
                    MessageBox.Show("设备连接初始化失败，请检查设备连接状态", "警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                controller = new TailInstallationController(commManager);

                LogManager.LogInfo("通讯管理器初始化完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化通讯管理器失败: {ex.Message}");
                MessageBox.Show($"初始化失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeLogging()
        {
            LogManager.LogInfo("系统启动完成");
        }

        #region 菜单按钮事件处理

        private void btnSystemMonitor_Click(object sender, EventArgs e)
        {
            if (!isLicenseValid) return;
            ShowSystemMonitor();
            UpdateMenuButtonState(btnSystemMonitor);
        }

        private void btnCommSettings_Click(object sender, EventArgs e)
        {
            if (!isLicenseValid) return;
            ShowCommunicationSettings();
            UpdateMenuButtonState(btnCommSettings);
        }

        private void btnDataView_Click(object sender, EventArgs e)
        {
            if (!isLicenseValid) return;
            ShowDataView();
            UpdateMenuButtonState(btnDataView);
        }

        private void btnUserManage_Click(object sender, EventArgs e)
        {
            if (!isLicenseValid) return;
            ShowUserManagement();
            UpdateMenuButtonState(btnUserManage);
        }

        private void btnSystemLog_Click(object sender, EventArgs e)
        {
            ShowSystemLog();
            UpdateMenuButtonState(btnSystemLog);
        }

        #endregion

        private void UpdateMenuButtonState(AntdUI.Button activeButton)
        {
            var buttons = new[] { btnSystemMonitor, btnCommSettings, btnDataView, btnUserManage, btnSystemLog };
            foreach (var btn in buttons)
            {
                btn.BackColor = Color.White;
                btn.ForeColor = Color.FromArgb(64, 64, 64);
                btn.Type = TTypeMini.Default;
            }
            activeButton.BackColor = Color.FromArgb(24, 144, 255);
            activeButton.ForeColor = Color.White;
            activeButton.Type = TTypeMini.Primary;
        }

        #region 界面切换方法

        private void ShowSystemMonitor()
        {
            if (systemMonitorControl == null)
            {
                systemMonitorControl = new View.SystemMonitorControl(controller, commManager);
                LogManager.LogInfo("系统监控界面已初始化");
            }
            SwitchUserControl(systemMonitorControl);
        }

        private void ShowCommunicationSettings()
        {
            var commSettingsControl = new CommunicationSettingsControl();

            // 绑定配置变更事件
            commSettingsControl.SettingsChanged += OnCommunicationSettingsChanged;

            SwitchUserControl(commSettingsControl);
        }

        private void ShowDataView()
        {
            var dataViewControl = new DataViewControl();
            SwitchUserControl(dataViewControl);
        }

        private void ShowUserManagement()
        {
            var userManageControl = new UserManagementControl();
            SwitchUserControl(userManageControl);
        }

        private void ShowSystemLog()
        {
            if (systemLogControl == null)
            {
                systemLogControl = new View.SystemLogControl();
            }
            SwitchUserControl(systemLogControl);
        }

        private void SwitchUserControl(UserControl newControl)
        {
            // 释放旧控件的事件订阅
            if (currentUserControl is CommunicationSettingsControl oldCommSettings)
            {
                oldCommSettings.SettingsChanged -= OnCommunicationSettingsChanged;
            }

            contentPanel.Controls.Clear();
            currentUserControl = newControl;
            newControl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(newControl);
        }

        #endregion

        #region 通讯事件处理方法

        /// <summary>
        /// 设备连接状态变化事件处理
        /// </summary>
        private void OnDeviceConnectionChanged(string deviceName, bool isConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, bool>(OnDeviceConnectionChanged), deviceName, isConnected);
                return;
            }

            string status = isConnected ? "已连接" : "断开连接";
            LogManager.LogInfo($"设备 {deviceName} 连接状态变化: {status}");

            // 如果当前显示的是系统监控界面，更新设备状态显示
            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                monitorControl.UpdateExternalDeviceStatus(deviceName, isConnected);
            }
        }

        /// <summary>
        /// 收到PC数据事件处理
        /// </summary>
        private void OnDataReceived(string data)
        {
            LogManager.LogInfo($"主窗口收到PC数据: {data.Substring(0, Math.Min(50, data.Length))}...");

            // 可以在这里处理接收到的数据
            // 比如更新界面显示、触发其他业务逻辑等
        }

        /// <summary>
        /// 扫码数据事件处理
        /// </summary>
        private void OnBarcodeScanned(string barcode)
        {
            LogManager.LogInfo($"主窗口收到扫码: {barcode}");

            // 如果当前是系统监控界面，可以更新当前产品信息
            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                monitorControl.UpdateCurrentProduct(barcode, "扫码完成");
            }
        }

        /// <summary>
        /// 拧紧轴数据事件处理 
        /// </summary>
        private void OnTighteningDataReceived(TighteningAxisData tighteningData)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TighteningAxisData>(OnTighteningDataReceived), tighteningData);
                return;
            }

            // 如果当前是系统监控界面，可以更新拧紧状态显示
            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                // 更新拧紧轴状态显示
                if (tighteningData.IsRunning)
                {
                    monitorControl.UpdateExternalDeviceStatus("TighteningAxis", true);
                }
                else if (tighteningData.IsOperationCompleted)
                {
                    // 操作完成，显示结果
                    string resultMessage = tighteningData.IsQualified ? "拧紧合格" : $"拧紧不合格: {tighteningData.QualityResult}";
                }

                // 如果有错误，记录错误信息
                if (tighteningData.HasError)
                {
                    LogManager.LogError($"拧紧轴错误: 错误代码{tighteningData.ErrorCode}");
                }
            }
        }

        /// <summary>
        /// PLC触发信号事件处理
        /// </summary>
        private void OnPLCTrigger(bool triggered)
        {
            if (triggered)
            {
                LogManager.LogInfo("主窗口收到PLC触发信号");

                // 可以在这里触发生产流程
                // 比如通知控制器开始执行尾椎安装
            }
        }

        #endregion

        #region 事件处理

        private void OnCommunicationSettingsChanged(object sender, EventArgs e)
        {
            try
            {
                LogManager.LogInfo("通讯配置已变更，准备更新...");

                // 检查系统是否正在运行
                bool systemRunning = controller != null && btnStop.Enabled;

                if (systemRunning)
                {
                    // 如果系统正在运行，提示用户
                    var result = MessageBox.Show(
                        "配置已保存。系统正在运行中，是否立即重启系统以应用新配置？\n\n" +
                        "选择\"是\"将停止并重启系统。\n" +
                        "选择\"否\"将在下次启动时应用新配置。",
                        "配置更新提示",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // 先停止系统
                        _ = Task.Run(async () =>
                        {
                            await controller.StopSystem();

                            // 在UI线程更新
                            this.Invoke(new Action(() =>
                            {
                                UpdateConfigurationOnly();
                                MessageBox.Show("配置已更新。请手动点击\"启动系统\"按钮重新启动。",
                                              "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        });
                    }
                    else
                    {
                        LogManager.LogInfo("配置已保存，将在下次系统启动时生效");
                    }
                }
                else
                {
                    // 系统未运行，直接更新配置
                    UpdateConfigurationOnly();
                    MessageBox.Show("配置已保存，将在启动系统时生效。",
                                  "配置保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"配置更新失败: {ex.Message}");
                MessageBox.Show($"配置更新失败: {ex.Message}", "错误",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateConfigurationOnly()
        {
            try
            {
                // 重新加载配置
                var newConfig = ConfigManager.LoadConfig();

                // 释放旧的通讯管理器（如果存在）
                if (commManager != null)
                {
                    // 先解绑事件
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnDataReceived -= OnDataReceived;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnTighteningDataReceived -= OnTighteningDataReceived;
                    commManager.OnPLCTrigger -= OnPLCTrigger;

                    // 释放资源
                    commManager.Dispose();
                }

                // 创建新的通讯管理器（但不初始化连接）
                commManager = new CommunicationManager(newConfig);

                // 重新绑定事件
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnDataReceived += OnDataReceived;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnTighteningDataReceived += OnTighteningDataReceived;
                commManager.OnPLCTrigger += OnPLCTrigger;

                // 更新控制器（如果存在）
                if (controller != null)
                {
                    controller = new TailInstallationController(commManager);
                }

                // 更新监控界面（如果存在）
                if (systemMonitorControl != null && currentUserControl is View.SystemMonitorControl)
                {
                    systemMonitorControl.Dispose();
                    systemMonitorControl = new View.SystemMonitorControl(controller, commManager);
                    SwitchUserControl(systemMonitorControl);
                }

                LogManager.LogInfo("配置已更新，等待系统启动");
                LogManager.LogInfo("========== 配置更新完成 ==========");
                LogManager.LogInfo($"当前系统状态: 待启动");
                LogManager.LogInfo($"设备配置已加载，等待用户启动系统");
                LogManager.LogInfo("==================================");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新配置时出错: {ex.Message}");
                throw;
            }
        }
        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (controller != null)
                {
                    controller.StopSystem();
                }

                if (commManager != null)
                {
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnDataReceived -= OnDataReceived;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnTighteningDataReceived -= OnTighteningDataReceived; 
                    commManager.OnPLCTrigger -= OnPLCTrigger;

                    commManager.Dispose();
                }

                LogManager.LogInfo("系统已关闭");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"关闭系统时出现异常: {ex.Message}");
            }

            base.OnFormClosing(e);
        }
    }
}
