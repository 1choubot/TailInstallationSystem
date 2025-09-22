using AntdUI;
using System;
using System.Drawing;
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

        public MainWindow()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeCommunication();
            InitializeLogging();
            // 启动时默认显示系统监控界面
            ShowSystemMonitor();
            UpdateMenuButtonState(btnSystemMonitor);

        }


        private void InitializeEvents()
        {
            // 菜单按钮事件已在设计器中绑定
        }

        private void InitializeCommunication()
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
                commManager.OnScrewDataReceived += OnScrewDataReceived;
                commManager.OnPLCTrigger += OnPLCTrigger;

                controller = new TailInstallationController(commManager);

                LogManager.LogInfo("通讯管理器初始化完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化通讯管理器失败: {ex.Message}");
            }
        }

        private void InitializeLogging()
        {
            LogManager.LogInfo("系统启动完成");
        }

        #region 菜单按钮事件处理

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
        /// 螺丝机数据事件处理
        /// </summary>
        private void OnScrewDataReceived(string screwData)
        {
            LogManager.LogInfo($"主窗口收到螺丝机数据: {screwData}");

            // 处理螺丝机返回的扭矩数据等
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
            // 配置变更后重新初始化通讯管理器
            InitializeCommunication();
            LogManager.LogInfo("通讯配置已更新，重新初始化通讯管理器");

            // 重新创建监控界面以使用新的通讯管理器
            systemMonitorControl?.Dispose();
            systemMonitorControl = new View.SystemMonitorControl(controller, commManager);
            if (currentUserControl is View.SystemMonitorControl)
            {
                SwitchUserControl(systemMonitorControl);
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

                // 取消事件订阅
                if (commManager != null)
                {
                    commManager.OnDeviceConnectionChanged -= OnDeviceConnectionChanged;
                    commManager.OnDataReceived -= OnDataReceived;
                    commManager.OnBarcodeScanned -= OnBarcodeScanned;
                    commManager.OnScrewDataReceived -= OnScrewDataReceived;
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