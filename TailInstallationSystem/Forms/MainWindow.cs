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

            // 启动时钟
            clockTimer.Start();
            UpdateClock(); // 立即更新一次显示当前时间

            if (isLicenseValid)
            {
                InitializeCommunicationManager();

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

        /// <summary>
        /// 初始化或重建通讯管理器
        /// </summary>
        private void InitializeCommunicationManager()
        {
            try
            {
                // 1. 如果旧实例存在，先释放
                if (commManager != null)
                {
                    LogManager.LogInfo("检测到旧的通讯管理器实例，准备释放...");

                    // 取消订阅事件
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnTighteningDataReceived -= OnTighteningDataReceived;
                    commManager.OnPLCTrigger -= OnPLCTrigger;

                    // 释放资源
                    commManager.Dispose();
                    commManager = null;

                    LogManager.LogInfo("旧通讯管理器已释放");

                    // 等待资源完全释放
                    System.Threading.Thread.Sleep(500);
                }

                // 2. 创建新实例
                var config = ConfigManager.LoadConfig();
                commManager = new CommunicationManager(config);
                LogManager.LogInfo("已创建新的通讯管理器实例");

                // 3. 订阅事件
                commManager.OnDeviceConnectionChanged += OnDeviceConnectionChanged;
                commManager.OnBarcodeScanned += OnBarcodeScanned;
                commManager.OnTighteningDataReceived += OnTighteningDataReceived;
                commManager.OnPLCTrigger += OnPLCTrigger;

                // 4. 更新控制器引用
                controller = new TailInstallationController(commManager);
                LogManager.LogInfo("控制器已更新为新的通讯管理器");

                // 5. 如果系统监控界面已存在，需要更新引用
                if (systemMonitorControl != null)
                {
                    // 取消订阅旧的重建请求事件
                    systemMonitorControl.RebuildManagerRequested -= OnRebuildManagerRequested;

                    // 重新创建系统监控界面
                    systemMonitorControl.Dispose();
                    systemMonitorControl = new View.SystemMonitorControl(controller, commManager);

                    // 订阅重建请求事件
                    systemMonitorControl.RebuildManagerRequested += OnRebuildManagerRequested;

                    // 如果当前显示的是监控界面，刷新显示
                    if (currentUserControl is View.SystemMonitorControl)
                    {
                        SwitchUserControl(systemMonitorControl);
                        LogManager.LogInfo("系统监控界面已更新");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化通讯管理器失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理系统监控界面的重建请求
        /// </summary>
        private void OnRebuildManagerRequested(object sender, EventArgs e)
        {
            try
            {
                LogManager.LogInfo("收到重建通讯管理器请求");
                InitializeCommunicationManager();
                LogManager.LogInfo("通讯管理器重建完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"重建通讯管理器失败: {ex.Message}");
                MessageBox.Show($"重建通讯管理器失败: {ex.Message}", "错误",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeEvents()
        {
            // 菜单按钮事件已在设计器中绑定
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
                systemMonitorControl.RebuildManagerRequested += OnRebuildManagerRequested;
                LogManager.LogInfo("系统监控界面已初始化");
            }
            SwitchUserControl(systemMonitorControl);
        }

        private void ShowCommunicationSettings()
        {
            var commSettingsControl = new CommunicationSettingsControl();
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
            if (currentUserControl is View.SystemMonitorControl oldMonitor)
            {
                oldMonitor.RebuildManagerRequested -= OnRebuildManagerRequested;
            }

            contentPanel.Controls.Clear();
            currentUserControl = newControl;
            newControl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(newControl);
        }

        #endregion

        #region 通讯事件处理方法

        private void OnDeviceConnectionChanged(string deviceName, bool isConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, bool>(OnDeviceConnectionChanged), deviceName, isConnected);
                return;
            }

            string status = isConnected ? "已连接" : "断开连接";
            LogManager.LogInfo($"设备 {deviceName} 连接状态变化: {status}");

            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                monitorControl.UpdateExternalDeviceStatus(deviceName, isConnected);
            }
        }

        private void OnBarcodeScanned(string barcode)
        {
            LogManager.LogInfo($"主窗口收到扫码: {barcode}");

            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                monitorControl.UpdateCurrentProduct(barcode, "扫码完成");
            }
        }

        private void OnTighteningDataReceived(TighteningAxisData tighteningData)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TighteningAxisData>(OnTighteningDataReceived), tighteningData);
                return;
            }

            if (currentUserControl is View.SystemMonitorControl monitorControl)
            {
                if (tighteningData.IsRunning)
                {
                    monitorControl.UpdateExternalDeviceStatus("TighteningAxis", true);
                }
                else if (tighteningData.IsOperationCompleted)
                {
                    string resultMessage = tighteningData.IsQualified ? "拧紧合格" : $"拧紧不合格: {tighteningData.QualityResult}";
                }

                if (tighteningData.IsOperationCompleted && !tighteningData.IsQualified)
                {
                    LogManager.LogWarning($"拧紧不合格: {tighteningData.QualityResult}, " +
                                         $"扭矩:{tighteningData.CompletedTorque:F2}Nm, " +
                                         $"角度:{Math.Abs(tighteningData.CompletedAngle):F1}°");
                }
            }
        }

        private void OnPLCTrigger(bool triggered)
        {
            if (triggered)
            {
                LogManager.LogInfo("主窗口收到PLC触发信号");
            }
        }

        #endregion

        #region 事件处理

        private void OnCommunicationSettingsChanged(object sender, EventArgs e)
        {
            try
            {
                LogManager.LogInfo("通讯配置已变更，准备更新...");

                bool systemRunning = controller != null && btnStop.Enabled;

                if (systemRunning)
                {
                    var result = MessageBox.Show(
                        "配置已保存。系统正在运行中，是否立即重启系统以应用新配置？\n\n" +
                        "选择\"是\"将停止并重启系统。\n" +
                        "选择\"否\"将在下次启动时应用新配置。",
                        "配置更新提示",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _ = Task.Run(async () =>
                        {
                            await controller.StopSystem();

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
                InitializeCommunicationManager();

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

        private void clockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            clockLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clockTimer?.Stop();
            clockTimer?.Dispose();

            try
            {
                if (controller != null)
                {
                    controller.StopSystem();
                }

                if (commManager != null)
                {
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnTighteningDataReceived -= OnTighteningDataReceived;
                    commManager.OnPLCTrigger -= OnPLCTrigger;

                    commManager.Dispose();

                    commManager = null;
                    LogManager.LogInfo("通讯管理器已释放并清空");
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
