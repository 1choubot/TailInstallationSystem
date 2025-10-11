namespace TailInstallationSystem.View
{
    partial class SystemMonitorControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.contentPanel = new AntdUI.Panel();
            this.logPanel = new AntdUI.Panel();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.btnClearLog = new AntdUI.Button();
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
            this.workModeDivider = new AntdUI.Divider();
            this.workModeLabel = new AntdUI.Label();
            this.workModeSwitch = new AntdUI.Switch();
            this.statusCards = new AntdUI.Panel();
            this.pcStatusCard = new AntdUI.Panel();
            this.pcIndicator = new AntdUI.Panel();
            this.pcStatusLabel = new AntdUI.Label();
            this.pcTitleLabel = new AntdUI.Label();
            this.screwStatusCard = new AntdUI.Panel();
            this.tighteningAxisIndicator = new AntdUI.Panel();
            this.tighteningAxisStatusLabel = new AntdUI.Label();
            this.screwTitleLabel = new AntdUI.Label();
            this.scannerStatusCard = new AntdUI.Panel();
            this.scannerIndicator = new AntdUI.Panel();
            this.scannerStatusLabel = new AntdUI.Label();
            this.scannerTitleLabel = new AntdUI.Label();
            this.plcStatusCard = new AntdUI.Panel();
            this.plcIndicator = new AntdUI.Panel();
            this.plcStatusLabel = new AntdUI.Label();
            this.plcTitleLabel = new AntdUI.Label();
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
            // contentPanel
            // 
            this.contentPanel.BackColor = System.Drawing.Color.White;
            this.contentPanel.Controls.Add(this.logPanel);
            this.contentPanel.Controls.Add(this.dataPanel);
            this.contentPanel.Controls.Add(this.controlPanel);
            this.contentPanel.Controls.Add(this.statusCards);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Padding = new System.Windows.Forms.Padding(20);
            this.contentPanel.Size = new System.Drawing.Size(1071, 719);
            this.contentPanel.TabIndex = 3;
            // 
            // logPanel
            // 
            this.logPanel.Controls.Add(this.logTextBox);
            this.logPanel.Controls.Add(this.btnClearLog);
            this.logPanel.Controls.Add(this.logTitle);
            this.logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logPanel.Location = new System.Drawing.Point(500, 220);
            this.logPanel.Name = "logPanel";
            this.logPanel.Size = new System.Drawing.Size(551, 479);
            this.logPanel.TabIndex = 3;
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
            this.logTextBox.Size = new System.Drawing.Size(551, 419);
            this.logTextBox.TabIndex = 3;
            // 
            // btnClearLog
            // 
            this.btnClearLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.btnClearLog.BorderWidth = 1F;
            this.btnClearLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnClearLog.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnClearLog.Location = new System.Drawing.Point(0, 449);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(551, 30);
            this.btnClearLog.TabIndex = 2;
            this.btnClearLog.Text = "清空日志";
            // 
            // logTitle
            // 
            this.logTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.logTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.logTitle.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.logTitle.Location = new System.Drawing.Point(0, 0);
            this.logTitle.Name = "logTitle";
            this.logTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.logTitle.Size = new System.Drawing.Size(551, 30);
            this.logTitle.TabIndex = 0;
            this.logTitle.Text = "系统日志";
            // 
            // dataPanel
            // 
            this.dataPanel.Controls.Add(this.currentProductPanel);
            this.dataPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.dataPanel.Location = new System.Drawing.Point(20, 220);
            this.dataPanel.Name = "dataPanel";
            this.dataPanel.Size = new System.Drawing.Size(480, 479);
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
            this.controlPanel.Controls.Add(this.workModeDivider);
            this.controlPanel.Controls.Add(this.workModeLabel);
            this.controlPanel.Controls.Add(this.workModeSwitch);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(20, 140);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(1031, 80);
            this.controlPanel.TabIndex = 1;
            // 
            // btnEmergencyStop
            // 
            this.btnEmergencyStop.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnEmergencyStop.Location = new System.Drawing.Point(800, 20);
            this.btnEmergencyStop.Name = "btnEmergencyStop";
            this.btnEmergencyStop.Size = new System.Drawing.Size(120, 45);
            this.btnEmergencyStop.TabIndex = 3;
            this.btnEmergencyStop.Text = "紧急停止";
            this.btnEmergencyStop.Type = AntdUI.TTypeMini.Error;
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
            // workModeDivider
            // 
            this.workModeDivider.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.workModeDivider.Location = new System.Drawing.Point(440, 20);
            this.workModeDivider.Name = "workModeDivider";
            this.workModeDivider.Vertical = true;
            this.workModeDivider.Size = new System.Drawing.Size(2, 45);
            this.workModeDivider.TabIndex = 20;
            // 
            // workModeLabel
            // 
            this.workModeLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.workModeLabel.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.workModeLabel.Location = new System.Drawing.Point(460, 23);
            this.workModeLabel.Name = "workModeLabel";
            this.workModeLabel.Size = new System.Drawing.Size(180, 30);
            this.workModeLabel.TabIndex = 21;
            this.workModeLabel.Text = "工作模式：完整流程";
            this.workModeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workModeSwitch
            // 
            this.workModeSwitch.AutoCheck = true;
            this.workModeSwitch.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.workModeSwitch.Location = new System.Drawing.Point(650, 25);
            this.workModeSwitch.Name = "workModeSwitch";
            this.workModeSwitch.Size = new System.Drawing.Size(60, 24);
            this.workModeSwitch.TabIndex = 22;
            this.workModeSwitch.Text = "";
            this.workModeSwitch.CheckedChanged += new AntdUI.BoolEventHandler(this.workModeSwitch_CheckedChanged);

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
            this.statusCards.Size = new System.Drawing.Size(1031, 120);
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
            this.screwStatusCard.Controls.Add(this.tighteningAxisIndicator);
            this.screwStatusCard.Controls.Add(this.tighteningAxisStatusLabel);
            this.screwStatusCard.Controls.Add(this.screwTitleLabel);
            this.screwStatusCard.Location = new System.Drawing.Point(480, 10);
            this.screwStatusCard.Name = "screwStatusCard";
            this.screwStatusCard.Size = new System.Drawing.Size(230, 100);
            this.screwStatusCard.TabIndex = 2;
            // 
            // tighteningAxisIndicator
            // 
            this.tighteningAxisIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.tighteningAxisIndicator.Location = new System.Drawing.Point(195, 20);
            this.tighteningAxisIndicator.Name = "tighteningAxisIndicator";
            this.tighteningAxisIndicator.Size = new System.Drawing.Size(12, 12);
            this.tighteningAxisIndicator.TabIndex = 2;
            // 
            // tighteningAxisStatusLabel
            // 
            this.tighteningAxisStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.tighteningAxisStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.tighteningAxisStatusLabel.Location = new System.Drawing.Point(15, 45);
            this.tighteningAxisStatusLabel.Name = "tighteningAxisStatusLabel";
            this.tighteningAxisStatusLabel.Size = new System.Drawing.Size(150, 23);
            this.tighteningAxisStatusLabel.TabIndex = 1;
            this.tighteningAxisStatusLabel.Text = "已连接";
            // 
            // screwTitleLabel
            // 
            this.screwTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.screwTitleLabel.Location = new System.Drawing.Point(15, 15);
            this.screwTitleLabel.Name = "screwTitleLabel";
            this.screwTitleLabel.Size = new System.Drawing.Size(150, 23);
            this.screwTitleLabel.TabIndex = 0;
            this.screwTitleLabel.Text = "拧紧轴";
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
            // SystemMonitorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.contentPanel);
            this.Name = "SystemMonitorControl";
            this.Size = new System.Drawing.Size(1071, 719);
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

        private AntdUI.Panel contentPanel;
        private AntdUI.Panel logPanel;
        private AntdUI.Button btnClearLog;
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
        private AntdUI.Panel tighteningAxisIndicator;
        private AntdUI.Label tighteningAxisStatusLabel;
        private AntdUI.Label screwTitleLabel;
        private AntdUI.Panel scannerStatusCard;
        private AntdUI.Panel scannerIndicator;
        private AntdUI.Label scannerStatusLabel;
        private AntdUI.Label scannerTitleLabel;
        private AntdUI.Panel plcStatusCard;
        private AntdUI.Panel plcIndicator;
        private AntdUI.Label plcStatusLabel;
        private AntdUI.Label plcTitleLabel;
        private System.Windows.Forms.TextBox logTextBox;
        private AntdUI.Switch workModeSwitch;
        private AntdUI.Label workModeLabel;
        private AntdUI.Divider workModeDivider;

    }
}
