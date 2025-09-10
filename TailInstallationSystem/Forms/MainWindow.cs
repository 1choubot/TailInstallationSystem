using AntdUI;
using System;
using System.Drawing;
using System.Windows.Forms;

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
            commManager = new CommunicationManager();
            controller = new TailInstallationController(commManager);
        }

        private void InitializeLogging()
        {
            LogManager.LogInfo("系统启动完成");
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

        private void ShowSystemMonitor()
        {
            // 切换到系统监控用户控件
            var monitorControl = new View.SystemMonitorControl(controller, commManager);
            SwitchUserControl(monitorControl);
        }

        private void ShowCommunicationSettings()
        {
            var commSettingsControl = new CommunicationSettingsControl();
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
            var logControl = new SystemLogControl();
            SwitchUserControl(logControl);
        }

        private void SwitchUserControl(UserControl newControl)
        {
            contentPanel.Controls.Clear();
            currentUserControl = newControl;
            newControl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(newControl);
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