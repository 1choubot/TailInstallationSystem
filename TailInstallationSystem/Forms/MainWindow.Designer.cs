namespace TailInstallationSystem
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.titleBar = new AntdUI.PageHeader();
            this.statusPanel = new AntdUI.Panel();
            this.statusLabel = new AntdUI.Label();
            this.headerPanel = new AntdUI.Panel();
            this.titleLabel = new AntdUI.Label();
            this.sidePanel = new AntdUI.Panel();
            this.menuPanel = new AntdUI.Panel();
            this.btnSystemLog = new AntdUI.Button();
            this.btnUserManage = new AntdUI.Button();
            this.btnDataView = new AntdUI.Button();
            this.btnCommSettings = new AntdUI.Button();
            this.btnSystemMonitor = new AntdUI.Button();
            this.contentPanel = new AntdUI.Panel();
            this.logPanel = new AntdUI.Panel();
            this.btnClearLog = new AntdUI.Button();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.logTitle = new AntdUI.Label();
            this.dataPanel = new AntdUI.Panel();
            this.currentProductPanel = new AntdUI.Panel();
            this.progressBar = new AntdUI.Progress();
            this.currentStatusLabel = new AntdUI.Label();
            this.currentBarcodeLabel = new AntdUI.Label();
            this.controlPanel = new AntdUI.Panel();
            this.btnEmergencyStop = new AntdUI.Button();
            this.btnSettings = new AntdUI.Button();
            this.btnStop = new AntdUI.Button();
            this.btnStart = new AntdUI.Button();
            this.statusCards = new AntdUI.Panel();
            this.pcStatusCard = new AntdUI.Panel();
            this.pcIndicator = new AntdUI.Panel();
            this.pcStatusLabel = new AntdUI.Label();
            this.pcTitleLabel = new AntdUI.Label();
            this.screwStatusCard = new AntdUI.Panel();
            this.screwIndicator = new AntdUI.Panel();
            this.screwStatusLabel = new AntdUI.Label();
            this.screwTitleLabel = new AntdUI.Label();
            this.scannerStatusCard = new AntdUI.Panel();
            this.scannerIndicator = new AntdUI.Panel();
            this.scannerStatusLabel = new AntdUI.Label();
            this.scannerTitleLabel = new AntdUI.Label();
            this.plcStatusCard = new AntdUI.Panel();
            this.plcIndicator = new AntdUI.Panel();
            this.plcStatusLabel = new AntdUI.Label();
            this.plcTitleLabel = new AntdUI.Label();
            this.statusPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.sidePanel.SuspendLayout();
            this.menuPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.logPanel.SuspendLayout();
            this.dataPanel.SuspendLayout();
            this.currentProductPanel.SuspendLayout();
            this.controlPanel.SuspendLayout();
            this.statusCards.SuspendLayout();
            this.pcStatusCard.SuspendLayout();
            this.screwStatusCard.SuspendLayout();
            this.scannerStatusCard.SuspendLayout();
            this.plcStatusCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleBar
            // 
            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Location = new System.Drawing.Point(0, 0);
            this.titleBar.Name = "titleBar";
            this.titleBar.ShowButton = true;
            this.titleBar.ShowIcon = true;
            this.titleBar.Size = new System.Drawing.Size(1200, 40);
            this.titleBar.SubText = "v1.0";
            this.titleBar.TabIndex = 0;
            this.titleBar.Text = "节点仪尾椎安装系统";
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(0, 770);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(1200, 30);
            this.statusPanel.TabIndex = 3;
            // 
            // statusLabel
            // 
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.statusLabel.Location = new System.Drawing.Point(0, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.statusLabel.Size = new System.Drawing.Size(1200, 30);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "系统就绪";
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 40);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1200, 60);
            this.headerPanel.TabIndex = 4;
            // 
            // titleLabel
            // 
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.titleLabel.Size = new System.Drawing.Size(1200, 60);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "节点仪尾椎安装系统 ";
            // 
            // sidePanel
            // 
            this.sidePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.sidePanel.Controls.Add(this.menuPanel);
            this.sidePanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidePanel.Location = new System.Drawing.Point(0, 100);
            this.sidePanel.Name = "sidePanel";
            this.sidePanel.Size = new System.Drawing.Size(200, 670);
            this.sidePanel.TabIndex = 5;
            // 
            // menuPanel
            // 
            this.menuPanel.Controls.Add(this.btnSystemLog);
            this.menuPanel.Controls.Add(this.btnUserManage);
            this.menuPanel.Controls.Add(this.btnDataView);
            this.menuPanel.Controls.Add(this.btnCommSettings);
            this.menuPanel.Controls.Add(this.btnSystemMonitor);
            this.menuPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.menuPanel.Location = new System.Drawing.Point(0, 0);
            this.menuPanel.Name = "menuPanel";
            this.menuPanel.Padding = new System.Windows.Forms.Padding(10);
            this.menuPanel.Size = new System.Drawing.Size(200, 670);
            this.menuPanel.TabIndex = 3;
            // 
            // btnSystemLog
            // 
            this.btnSystemLog.BackColor = System.Drawing.Color.White;
            this.btnSystemLog.BorderWidth = 1F;
            this.btnSystemLog.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSystemLog.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnSystemLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnSystemLog.Location = new System.Drawing.Point(10, 170);
            this.btnSystemLog.Name = "btnSystemLog";
            this.btnSystemLog.Size = new System.Drawing.Size(180, 40);
            this.btnSystemLog.TabIndex = 4;
            this.btnSystemLog.Text = "系统日志";
            this.btnSystemLog.Click += new System.EventHandler(this.btnSystemLog_Click);
            // 
            // btnUserManage
            // 
            this.btnUserManage.BackColor = System.Drawing.Color.White;
            this.btnUserManage.BorderWidth = 1F;
            this.btnUserManage.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnUserManage.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnUserManage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnUserManage.Location = new System.Drawing.Point(10, 130);
            this.btnUserManage.Name = "btnUserManage";
            this.btnUserManage.Size = new System.Drawing.Size(180, 40);
            this.btnUserManage.TabIndex = 3;
            this.btnUserManage.Text = "用户管理";
            this.btnUserManage.Click += new System.EventHandler(this.btnUserManage_Click);
            // 
            // btnDataView
            // 
            this.btnDataView.BackColor = System.Drawing.Color.White;
            this.btnDataView.BorderWidth = 1F;
            this.btnDataView.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnDataView.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnDataView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnDataView.Location = new System.Drawing.Point(10, 90);
            this.btnDataView.Name = "btnDataView";
            this.btnDataView.Size = new System.Drawing.Size(180, 40);
            this.btnDataView.TabIndex = 2;
            this.btnDataView.Text = "数据查看";
            this.btnDataView.Click += new System.EventHandler(this.btnDataView_Click);
            // 
            // btnCommSettings
            // 
            this.btnCommSettings.BackColor = System.Drawing.Color.White;
            this.btnCommSettings.BorderWidth = 1F;
            this.btnCommSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCommSettings.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnCommSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnCommSettings.Location = new System.Drawing.Point(10, 50);
            this.btnCommSettings.Name = "btnCommSettings";
            this.btnCommSettings.Size = new System.Drawing.Size(180, 40);
            this.btnCommSettings.TabIndex = 1;
            this.btnCommSettings.Text = "通讯设置";
            this.btnCommSettings.Click += new System.EventHandler(this.btnCommSettings_Click);
            // 
            // btnSystemMonitor
            // 
            this.btnSystemMonitor.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.btnSystemMonitor.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSystemMonitor.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnSystemMonitor.ForeColor = System.Drawing.Color.White;
            this.btnSystemMonitor.Location = new System.Drawing.Point(10, 10);
            this.btnSystemMonitor.Name = "btnSystemMonitor";
            this.btnSystemMonitor.Size = new System.Drawing.Size(180, 40);
            this.btnSystemMonitor.TabIndex = 0;
            this.btnSystemMonitor.Text = "系统监控";
            this.btnSystemMonitor.Type = AntdUI.TTypeMini.Primary;
            this.btnSystemMonitor.Click += new System.EventHandler(this.btnSystemMonitor_Click);
            // 
            // contentPanel
            // 
            this.contentPanel.BackColor = System.Drawing.Color.White;
            this.contentPanel.Controls.Add(this.logPanel);
            this.contentPanel.Controls.Add(this.dataPanel);
            this.contentPanel.Controls.Add(this.controlPanel);
            this.contentPanel.Controls.Add(this.statusCards);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(200, 100);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Padding = new System.Windows.Forms.Padding(20);
            this.contentPanel.Size = new System.Drawing.Size(1000, 670);
            this.contentPanel.TabIndex = 6;
            // 
            // logPanel
            // 
            this.logPanel.Controls.Add(this.btnClearLog);
            this.logPanel.Controls.Add(this.logTextBox);
            this.logPanel.Controls.Add(this.logTitle);
            this.logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logPanel.Location = new System.Drawing.Point(500, 220);
            this.logPanel.Name = "logPanel";
            this.logPanel.Size = new System.Drawing.Size(480, 430);
            this.logPanel.TabIndex = 3;
            // 
            // btnClearLog
            // 
            this.btnClearLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.btnClearLog.BorderWidth = 1F;
            this.btnClearLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnClearLog.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnClearLog.Location = new System.Drawing.Point(0, 400);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(480, 30);
            this.btnClearLog.TabIndex = 2;
            this.btnClearLog.Text = "清空日志";
            // 
            // logTextBox
            // 
            this.logTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.logTextBox.Location = new System.Drawing.Point(0, 30);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(480, 400);
            this.logTextBox.TabIndex = 1;
            // 
            // logTitle
            // 
            this.logTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.logTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.logTitle.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.logTitle.Location = new System.Drawing.Point(0, 0);
            this.logTitle.Name = "logTitle";
            this.logTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.logTitle.Size = new System.Drawing.Size(480, 30);
            this.logTitle.TabIndex = 0;
            this.logTitle.Text = "系统日志";
            // 
            // dataPanel
            // 
            this.dataPanel.Controls.Add(this.currentProductPanel);
            this.dataPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.dataPanel.Location = new System.Drawing.Point(20, 220);
            this.dataPanel.Name = "dataPanel";
            this.dataPanel.Size = new System.Drawing.Size(480, 430);
            this.dataPanel.TabIndex = 2;
            // 
            // currentProductPanel
            // 
            this.currentProductPanel.Controls.Add(this.progressBar);
            this.currentProductPanel.Controls.Add(this.currentStatusLabel);
            this.currentProductPanel.Controls.Add(this.currentBarcodeLabel);
            this.currentProductPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentProductPanel.Location = new System.Drawing.Point(0, 0);
            this.currentProductPanel.Name = "currentProductPanel";
            this.currentProductPanel.Padding = new System.Windows.Forms.Padding(20);
            this.currentProductPanel.Size = new System.Drawing.Size(480, 150);
            this.currentProductPanel.TabIndex = 0;
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar.Location = new System.Drawing.Point(20, 75);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(440, 25);
            this.progressBar.TabIndex = 2;
            // 
            // currentStatusLabel
            // 
            this.currentStatusLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.currentStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.currentStatusLabel.Location = new System.Drawing.Point(20, 50);
            this.currentStatusLabel.Name = "currentStatusLabel";
            this.currentStatusLabel.Size = new System.Drawing.Size(440, 25);
            this.currentStatusLabel.TabIndex = 1;
            this.currentStatusLabel.Text = "状态: 待机中";
            // 
            // currentBarcodeLabel
            // 
            this.currentBarcodeLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentBarcodeLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.currentBarcodeLabel.Location = new System.Drawing.Point(20, 20);
            this.currentBarcodeLabel.Name = "currentBarcodeLabel";
            this.currentBarcodeLabel.Size = new System.Drawing.Size(440, 30);
            this.currentBarcodeLabel.TabIndex = 0;
            this.currentBarcodeLabel.Text = "当前产品条码: 等待扫描...";
            // 
            // controlPanel
            // 
            this.controlPanel.Controls.Add(this.btnEmergencyStop);
            this.controlPanel.Controls.Add(this.btnSettings);
            this.controlPanel.Controls.Add(this.btnStop);
            this.controlPanel.Controls.Add(this.btnStart);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(20, 140);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(960, 80);
            this.controlPanel.TabIndex = 1;
            // 
            // btnEmergencyStop
            // 
            this.btnEmergencyStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnEmergencyStop.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnEmergencyStop.ForeColor = System.Drawing.Color.White;
            this.btnEmergencyStop.Location = new System.Drawing.Point(800, 20);
            this.btnEmergencyStop.Name = "btnEmergencyStop";
            this.btnEmergencyStop.Size = new System.Drawing.Size(120, 45);
            this.btnEmergencyStop.TabIndex = 3;
            this.btnEmergencyStop.Text = "🚨 紧急停止";
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.btnSettings.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.Location = new System.Drawing.Point(280, 20);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(120, 45);
            this.btnSettings.TabIndex = 2;
            this.btnSettings.Text = "系统设置";
            this.btnSettings.Type = AntdUI.TTypeMini.Primary;
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.btnStop.Enabled = false;
            this.btnStop.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(140, 20);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(120, 45);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "停止系统";
            this.btnStop.Type = AntdUI.TTypeMini.Error;
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(0, 20);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(120, 45);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "启动系统";
            this.btnStart.Type = AntdUI.TTypeMini.Success;
            // 
            // statusCards
            // 
            this.statusCards.Controls.Add(this.pcStatusCard);
            this.statusCards.Controls.Add(this.screwStatusCard);
            this.statusCards.Controls.Add(this.scannerStatusCard);
            this.statusCards.Controls.Add(this.plcStatusCard);
            this.statusCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.statusCards.Location = new System.Drawing.Point(20, 20);
            this.statusCards.Name = "statusCards";
            this.statusCards.Size = new System.Drawing.Size(960, 120);
            this.statusCards.TabIndex = 0;
            // 
            // pcStatusCard
            // 
            this.pcStatusCard.BackColor = System.Drawing.Color.White;
            this.pcStatusCard.Controls.Add(this.pcIndicator);
            this.pcStatusCard.Controls.Add(this.pcStatusLabel);
            this.pcStatusCard.Controls.Add(this.pcTitleLabel);
            this.pcStatusCard.Location = new System.Drawing.Point(720, 10);
            this.pcStatusCard.Name = "pcStatusCard";
            this.pcStatusCard.Size = new System.Drawing.Size(230, 100);
            this.pcStatusCard.TabIndex = 3;
            // 
            // pcIndicator
            // 
            this.pcIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(173)))), ((int)(((byte)(20)))));
            this.pcIndicator.Location = new System.Drawing.Point(195, 20);
            this.pcIndicator.Name = "pcIndicator";
            this.pcIndicator.Size = new System.Drawing.Size(12, 12);
            this.pcIndicator.TabIndex = 2;
            // 
            // pcStatusLabel
            // 
            this.pcStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.pcStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(173)))), ((int)(((byte)(20)))));
            this.pcStatusLabel.Location = new System.Drawing.Point(15, 45);
            this.pcStatusLabel.Name = "pcStatusLabel";
            this.pcStatusLabel.Size = new System.Drawing.Size(150, 23);
            this.pcStatusLabel.TabIndex = 1;
            this.pcStatusLabel.Text = "等待数据";
            // 
            // pcTitleLabel
            // 
            this.pcTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.pcTitleLabel.Location = new System.Drawing.Point(15, 15);
            this.pcTitleLabel.Name = "pcTitleLabel";
            this.pcTitleLabel.Size = new System.Drawing.Size(150, 23);
            this.pcTitleLabel.TabIndex = 0;
            this.pcTitleLabel.Text = "PC通讯";
            // 
            // screwStatusCard
            // 
            this.screwStatusCard.BackColor = System.Drawing.Color.White;
            this.screwStatusCard.Controls.Add(this.screwIndicator);
            this.screwStatusCard.Controls.Add(this.screwStatusLabel);
            this.screwStatusCard.Controls.Add(this.screwTitleLabel);
            this.screwStatusCard.Location = new System.Drawing.Point(480, 10);
            this.screwStatusCard.Name = "screwStatusCard";
            this.screwStatusCard.Size = new System.Drawing.Size(230, 100);
            this.screwStatusCard.TabIndex = 2;
            // 
            // screwIndicator
            // 
            this.screwIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.screwIndicator.Location = new System.Drawing.Point(195, 20);
            this.screwIndicator.Name = "screwIndicator";
            this.screwIndicator.Size = new System.Drawing.Size(12, 12);
            this.screwIndicator.TabIndex = 2;
            // 
            // screwStatusLabel
            // 
            this.screwStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.screwStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.screwStatusLabel.Location = new System.Drawing.Point(15, 45);
            this.screwStatusLabel.Name = "screwStatusLabel";
            this.screwStatusLabel.Size = new System.Drawing.Size(150, 23);
            this.screwStatusLabel.TabIndex = 1;
            this.screwStatusLabel.Text = "已连接";
            // 
            // screwTitleLabel
            // 
            this.screwTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.screwTitleLabel.Location = new System.Drawing.Point(15, 15);
            this.screwTitleLabel.Name = "screwTitleLabel";
            this.screwTitleLabel.Size = new System.Drawing.Size(150, 23);
            this.screwTitleLabel.TabIndex = 0;
            this.screwTitleLabel.Text = "螺丝机";
            // 
            // scannerStatusCard
            // 
            this.scannerStatusCard.BackColor = System.Drawing.Color.White;
            this.scannerStatusCard.Controls.Add(this.scannerIndicator);
            this.scannerStatusCard.Controls.Add(this.scannerStatusLabel);
            this.scannerStatusCard.Controls.Add(this.scannerTitleLabel);
            this.scannerStatusCard.Location = new System.Drawing.Point(240, 10);
            this.scannerStatusCard.Name = "scannerStatusCard";
            this.scannerStatusCard.Size = new System.Drawing.Size(230, 100);
            this.scannerStatusCard.TabIndex = 1;
            // 
            // scannerIndicator
            // 
            this.scannerIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.scannerIndicator.Location = new System.Drawing.Point(195, 20);
            this.scannerIndicator.Name = "scannerIndicator";
            this.scannerIndicator.Size = new System.Drawing.Size(12, 12);
            this.scannerIndicator.TabIndex = 2;
            // 
            // scannerStatusLabel
            // 
            this.scannerStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.scannerStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.scannerStatusLabel.Location = new System.Drawing.Point(15, 45);
            this.scannerStatusLabel.Name = "scannerStatusLabel";
            this.scannerStatusLabel.Size = new System.Drawing.Size(150, 23);
            this.scannerStatusLabel.TabIndex = 1;
            this.scannerStatusLabel.Text = "已连接";
            // 
            // scannerTitleLabel
            // 
            this.scannerTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.scannerTitleLabel.Location = new System.Drawing.Point(15, 15);
            this.scannerTitleLabel.Name = "scannerTitleLabel";
            this.scannerTitleLabel.Size = new System.Drawing.Size(150, 23);
            this.scannerTitleLabel.TabIndex = 0;
            this.scannerTitleLabel.Text = "扫码枪";
            // 
            // plcStatusCard
            // 
            this.plcStatusCard.BackColor = System.Drawing.Color.White;
            this.plcStatusCard.Controls.Add(this.plcIndicator);
            this.plcStatusCard.Controls.Add(this.plcStatusLabel);
            this.plcStatusCard.Controls.Add(this.plcTitleLabel);
            this.plcStatusCard.Location = new System.Drawing.Point(0, 10);
            this.plcStatusCard.Name = "plcStatusCard";
            this.plcStatusCard.Size = new System.Drawing.Size(230, 100);
            this.plcStatusCard.TabIndex = 0;
            // 
            // plcIndicator
            // 
            this.plcIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.plcIndicator.Location = new System.Drawing.Point(195, 20);
            this.plcIndicator.Name = "plcIndicator";
            this.plcIndicator.Size = new System.Drawing.Size(12, 12);
            this.plcIndicator.TabIndex = 2;
            // 
            // plcStatusLabel
            // 
            this.plcStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.plcStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.plcStatusLabel.Location = new System.Drawing.Point(15, 45);
            this.plcStatusLabel.Name = "plcStatusLabel";
            this.plcStatusLabel.Size = new System.Drawing.Size(150, 23);
            this.plcStatusLabel.TabIndex = 1;
            this.plcStatusLabel.Text = "已连接";
            // 
            // plcTitleLabel
            // 
            this.plcTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.plcTitleLabel.Location = new System.Drawing.Point(15, 15);
            this.plcTitleLabel.Name = "plcTitleLabel";
            this.plcTitleLabel.Size = new System.Drawing.Size(150, 23);
            this.plcTitleLabel.TabIndex = 0;
            this.plcTitleLabel.Text = "PLC连接";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.ControlBox = false;
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.sidePanel);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.titleBar);
            this.Controls.Add(this.statusPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "节点仪尾椎安装系统";
            this.statusPanel.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.sidePanel.ResumeLayout(false);
            this.menuPanel.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.logPanel.ResumeLayout(false);
            this.logPanel.PerformLayout();
            this.dataPanel.ResumeLayout(false);
            this.currentProductPanel.ResumeLayout(false);
            this.controlPanel.ResumeLayout(false);
            this.statusCards.ResumeLayout(false);
            this.pcStatusCard.ResumeLayout(false);
            this.screwStatusCard.ResumeLayout(false);
            this.scannerStatusCard.ResumeLayout(false);
            this.plcStatusCard.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private AntdUI.PageHeader titleBar;
        private AntdUI.Panel statusPanel;
        private AntdUI.Label statusLabel;
        private AntdUI.Panel headerPanel;
        private AntdUI.Label titleLabel;
        private AntdUI.Panel sidePanel;
        private AntdUI.Panel menuPanel;
        private AntdUI.Button btnSystemLog;
        private AntdUI.Button btnUserManage;
        private AntdUI.Button btnDataView;
        private AntdUI.Button btnCommSettings;
        private AntdUI.Button btnSystemMonitor;
        private AntdUI.Panel contentPanel;
        private AntdUI.Panel logPanel;
        private AntdUI.Button btnClearLog;
        private System.Windows.Forms.TextBox logTextBox;
        private AntdUI.Label logTitle;
        private AntdUI.Panel dataPanel;
        private AntdUI.Panel currentProductPanel;
        private AntdUI.Progress progressBar;
        private AntdUI.Label currentStatusLabel;
        private AntdUI.Label currentBarcodeLabel;
        private AntdUI.Panel controlPanel;
        private AntdUI.Button btnEmergencyStop;
        private AntdUI.Button btnSettings;
        private AntdUI.Button btnStop;
        private AntdUI.Button btnStart;
        private AntdUI.Panel statusCards;
        private AntdUI.Panel pcStatusCard;
        private AntdUI.Panel pcIndicator;
        private AntdUI.Label pcStatusLabel;
        private AntdUI.Label pcTitleLabel;
        private AntdUI.Panel screwStatusCard;
        private AntdUI.Panel screwIndicator;
        private AntdUI.Label screwStatusLabel;
        private AntdUI.Label screwTitleLabel;
        private AntdUI.Panel scannerStatusCard;
        private AntdUI.Panel scannerIndicator;
        private AntdUI.Label scannerStatusLabel;
        private AntdUI.Label scannerTitleLabel;
        private AntdUI.Panel plcStatusCard;
        private AntdUI.Panel plcIndicator;
        private AntdUI.Label plcStatusLabel;
        private AntdUI.Label plcTitleLabel;
    }
}