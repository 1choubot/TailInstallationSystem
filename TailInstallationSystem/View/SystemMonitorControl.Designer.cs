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
            this.tighteningDataPanel = new AntdUI.Panel();
            this.lblLastProduct = new AntdUI.Label();
            this.lblRunningStatus = new AntdUI.Label();
            this.torqueDivider = new AntdUI.Divider();
            this.lblTorqueRange = new AntdUI.Label();
            this.lblCompletedTorque = new AntdUI.Label();
            this.lblRealtimeTorque = new AntdUI.Label();
            this.lblTargetTorque = new AntdUI.Label();
            this.tighteningDataTitle = new AntdUI.Label();
            this.currentProductPanel = new AntdUI.Panel();
            this.progressBar = new AntdUI.Progress();
            this.currentStatusLabel = new AntdUI.Label();
            this.currentBarcodeLabel = new AntdUI.Label();
            this.controlPanel = new AntdUI.Panel();
            this.btnEmergencyStop = new AntdUI.Button();
            this.btnSettings = new AntdUI.Button();
            this.btnStop = new AntdUI.Button();
            this.btnStart = new AntdUI.Button();
            this.statusCards = new System.Windows.Forms.TableLayoutPanel();
            this.plcStatusCard = new AntdUI.Panel();
            this.plcIndicator = new AntdUI.Panel();
            this.plcStatusLabel = new AntdUI.Label();
            this.plcTitleLabel = new AntdUI.Label();
            this.scannerStatusCard = new AntdUI.Panel();
            this.scannerIndicator = new AntdUI.Panel();
            this.scannerStatusLabel = new AntdUI.Label();
            this.scannerTitleLabel = new AntdUI.Label();
            this.screwStatusCard = new AntdUI.Panel();
            this.tighteningAxisIndicator = new AntdUI.Panel();
            this.tighteningAxisStatusLabel = new AntdUI.Label();
            this.screwTitleLabel = new AntdUI.Label();
            this.contentPanel.SuspendLayout();
            this.logPanel.SuspendLayout();
            this.dataPanel.SuspendLayout();
            this.tighteningDataPanel.SuspendLayout();
            this.currentProductPanel.SuspendLayout();
            this.controlPanel.SuspendLayout();
            this.statusCards.SuspendLayout();
            this.plcStatusCard.SuspendLayout();
            this.scannerStatusCard.SuspendLayout();
            this.screwStatusCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // contentPanel
            // 
            this.contentPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
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
            this.logPanel.BackColor = System.Drawing.Color.White;
            this.logPanel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this.logPanel.BorderWidth = 1F;
            this.logPanel.Controls.Add(this.logTextBox);
            this.logPanel.Controls.Add(this.btnClearLog);
            this.logPanel.Controls.Add(this.logTitle);
            this.logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logPanel.Location = new System.Drawing.Point(500, 220);
            this.logPanel.Name = "logPanel";
            this.logPanel.Radius = 8;
            this.logPanel.Shadow = 2;
            this.logPanel.Size = new System.Drawing.Size(551, 479);
            this.logPanel.TabIndex = 3;
            // 
            // logTextBox
            // 
            this.logTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.logTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.logTextBox.Location = new System.Drawing.Point(4, 34);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logTextBox.Size = new System.Drawing.Size(543, 401);
            this.logTextBox.TabIndex = 3;
            // 
            // btnClearLog
            // 
            this.btnClearLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.btnClearLog.BorderWidth = 1F;
            this.btnClearLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnClearLog.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnClearLog.Location = new System.Drawing.Point(4, 435);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(543, 40);
            this.btnClearLog.TabIndex = 2;
            this.btnClearLog.Text = "清空日志";
            // 
            // logTitle
            // 
            this.logTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.logTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.logTitle.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.logTitle.Location = new System.Drawing.Point(4, 4);
            this.logTitle.Name = "logTitle";
            this.logTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.logTitle.Size = new System.Drawing.Size(543, 30);
            this.logTitle.TabIndex = 0;
            this.logTitle.Text = "系统日志";
            // 
            // dataPanel
            // 
            this.dataPanel.Controls.Add(this.tighteningDataPanel);
            this.dataPanel.Controls.Add(this.currentProductPanel);
            this.dataPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.dataPanel.Location = new System.Drawing.Point(20, 220);
            this.dataPanel.Name = "dataPanel";
            this.dataPanel.Size = new System.Drawing.Size(480, 479);
            this.dataPanel.TabIndex = 2;
            // 
            // tighteningDataPanel
            // 
            this.tighteningDataPanel.BackColor = System.Drawing.Color.White;
            this.tighteningDataPanel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this.tighteningDataPanel.BorderWidth = 1F;
            this.tighteningDataPanel.Controls.Add(this.lblLastProduct);
            this.tighteningDataPanel.Controls.Add(this.lblRunningStatus);
            this.tighteningDataPanel.Controls.Add(this.torqueDivider);
            this.tighteningDataPanel.Controls.Add(this.lblTorqueRange);
            this.tighteningDataPanel.Controls.Add(this.lblCompletedTorque);
            this.tighteningDataPanel.Controls.Add(this.lblRealtimeTorque);
            this.tighteningDataPanel.Controls.Add(this.lblTargetTorque);
            this.tighteningDataPanel.Controls.Add(this.tighteningDataTitle);
            this.tighteningDataPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tighteningDataPanel.Location = new System.Drawing.Point(0, 115);
            this.tighteningDataPanel.Name = "tighteningDataPanel";
            this.tighteningDataPanel.Padding = new System.Windows.Forms.Padding(15, 8, 15, 15);
            this.tighteningDataPanel.Radius = 8;
            this.tighteningDataPanel.Shadow = 2;
            this.tighteningDataPanel.Size = new System.Drawing.Size(480, 364);
            this.tighteningDataPanel.TabIndex = 1;
            // 
            // lblLastProduct
            // 
            this.lblLastProduct.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLastProduct.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.lblLastProduct.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblLastProduct.Location = new System.Drawing.Point(19, 166);
            this.lblLastProduct.Name = "lblLastProduct";
            this.lblLastProduct.Padding = new System.Windows.Forms.Padding(12, 2, 0, 2);
            this.lblLastProduct.Size = new System.Drawing.Size(442, 24);
            this.lblLastProduct.TabIndex = 7;
            this.lblLastProduct.Text = "最近拧紧：--";
            // 
            // lblRunningStatus
            // 
            this.lblRunningStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRunningStatus.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.lblRunningStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.lblRunningStatus.Location = new System.Drawing.Point(19, 141);
            this.lblRunningStatus.Name = "lblRunningStatus";
            this.lblRunningStatus.Padding = new System.Windows.Forms.Padding(12, 3, 0, 2);
            this.lblRunningStatus.Size = new System.Drawing.Size(442, 25);
            this.lblRunningStatus.TabIndex = 6;
            this.lblRunningStatus.Text = "运行状态：未连接";
            // 
            // torqueDivider
            // 
            this.torqueDivider.Dock = System.Windows.Forms.DockStyle.Top;
            this.torqueDivider.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.torqueDivider.Location = new System.Drawing.Point(19, 140);
            this.torqueDivider.Margin = new System.Windows.Forms.Padding(12, 3, 12, 3);
            this.torqueDivider.Name = "torqueDivider";
            this.torqueDivider.Size = new System.Drawing.Size(442, 1);
            this.torqueDivider.TabIndex = 5;
            // 
            // lblTorqueRange
            // 
            this.lblTorqueRange.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTorqueRange.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.lblTorqueRange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.lblTorqueRange.Location = new System.Drawing.Point(19, 116);
            this.lblTorqueRange.Name = "lblTorqueRange";
            this.lblTorqueRange.Padding = new System.Windows.Forms.Padding(12, 2, 0, 2);
            this.lblTorqueRange.Size = new System.Drawing.Size(442, 24);
            this.lblTorqueRange.TabIndex = 4;
            this.lblTorqueRange.Text = "扭矩范围：-- ~ -- Nm";
            // 
            // lblCompletedTorque
            // 
            this.lblCompletedTorque.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCompletedTorque.Font = new System.Drawing.Font("微软雅黑", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCompletedTorque.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompletedTorque.Location = new System.Drawing.Point(19, 92);
            this.lblCompletedTorque.Name = "lblCompletedTorque";
            this.lblCompletedTorque.Padding = new System.Windows.Forms.Padding(12, 2, 0, 2);
            this.lblCompletedTorque.Size = new System.Drawing.Size(442, 24);
            this.lblCompletedTorque.TabIndex = 3;
            this.lblCompletedTorque.Text = "完成扭矩：-- Nm";
            // 
            // lblRealtimeTorque
            // 
            this.lblRealtimeTorque.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRealtimeTorque.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.lblRealtimeTorque.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.lblRealtimeTorque.Location = new System.Drawing.Point(19, 68);
            this.lblRealtimeTorque.Name = "lblRealtimeTorque";
            this.lblRealtimeTorque.Padding = new System.Windows.Forms.Padding(12, 2, 0, 2);
            this.lblRealtimeTorque.Size = new System.Drawing.Size(442, 24);
            this.lblRealtimeTorque.TabIndex = 2;
            this.lblRealtimeTorque.Text = "实时扭矩：0.0 Nm";
            // 
            // lblTargetTorque
            // 
            this.lblTargetTorque.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTargetTorque.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.lblTargetTorque.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblTargetTorque.Location = new System.Drawing.Point(19, 44);
            this.lblTargetTorque.Name = "lblTargetTorque";
            this.lblTargetTorque.Padding = new System.Windows.Forms.Padding(12, 2, 0, 2);
            this.lblTargetTorque.Size = new System.Drawing.Size(442, 24);
            this.lblTargetTorque.TabIndex = 1;
            this.lblTargetTorque.Text = "目标扭矩：-- Nm";
            // 
            // tighteningDataTitle
            // 
            this.tighteningDataTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.tighteningDataTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.tighteningDataTitle.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.tighteningDataTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.tighteningDataTitle.Location = new System.Drawing.Point(19, 12);
            this.tighteningDataTitle.Name = "tighteningDataTitle";
            this.tighteningDataTitle.Padding = new System.Windows.Forms.Padding(12, 7, 0, 7);
            this.tighteningDataTitle.Size = new System.Drawing.Size(442, 32);
            this.tighteningDataTitle.TabIndex = 0;
            this.tighteningDataTitle.Text = "拧紧轴配置";
            // 
            // currentProductPanel
            // 
            this.currentProductPanel.BackColor = System.Drawing.Color.White;
            this.currentProductPanel.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this.currentProductPanel.BorderWidth = 1F;
            this.currentProductPanel.Controls.Add(this.progressBar);
            this.currentProductPanel.Controls.Add(this.currentStatusLabel);
            this.currentProductPanel.Controls.Add(this.currentBarcodeLabel);
            this.currentProductPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentProductPanel.Location = new System.Drawing.Point(0, 0);
            this.currentProductPanel.Name = "currentProductPanel";
            this.currentProductPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.currentProductPanel.Radius = 8;
            this.currentProductPanel.Shadow = 2;
            this.currentProductPanel.Size = new System.Drawing.Size(480, 115);
            this.currentProductPanel.TabIndex = 0;
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar.Location = new System.Drawing.Point(24, 79);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(432, 25);
            this.progressBar.TabIndex = 2;
            // 
            // currentStatusLabel
            // 
            this.currentStatusLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10.5F);
            this.currentStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.currentStatusLabel.Location = new System.Drawing.Point(24, 54);
            this.currentStatusLabel.Name = "currentStatusLabel";
            this.currentStatusLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.currentStatusLabel.Size = new System.Drawing.Size(432, 25);
            this.currentStatusLabel.TabIndex = 1;
            this.currentStatusLabel.Text = "状态: 待机中";
            // 
            // currentBarcodeLabel
            // 
            this.currentBarcodeLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.currentBarcodeLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentBarcodeLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.currentBarcodeLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.currentBarcodeLabel.Location = new System.Drawing.Point(24, 19);
            this.currentBarcodeLabel.Name = "currentBarcodeLabel";
            this.currentBarcodeLabel.Padding = new System.Windows.Forms.Padding(8, 5, 8, 5);
            this.currentBarcodeLabel.Size = new System.Drawing.Size(432, 35);
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
            this.controlPanel.Size = new System.Drawing.Size(1031, 80);
            this.controlPanel.TabIndex = 1;
            // 
            // btnEmergencyStop
            // 
            this.btnEmergencyStop.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.btnEmergencyStop.Location = new System.Drawing.Point(420, 20);
            this.btnEmergencyStop.Name = "btnEmergencyStop";
            this.btnEmergencyStop.Size = new System.Drawing.Size(120, 45);
            this.btnEmergencyStop.TabIndex = 3;
            this.btnEmergencyStop.Text = "紧急停止";
            this.btnEmergencyStop.Type = AntdUI.TTypeMini.Error;
            this.btnEmergencyStop.Visible = false;
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
            this.statusCards.BackColor = System.Drawing.Color.White;
            this.statusCards.ColumnCount = 3;
            this.statusCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.statusCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.statusCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.statusCards.Controls.Add(this.plcStatusCard, 0, 0);
            this.statusCards.Controls.Add(this.scannerStatusCard, 1, 0);
            this.statusCards.Controls.Add(this.screwStatusCard, 2, 0);
            this.statusCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.statusCards.Location = new System.Drawing.Point(20, 20);
            this.statusCards.Name = "statusCards";
            this.statusCards.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            this.statusCards.RowCount = 1;
            this.statusCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statusCards.Size = new System.Drawing.Size(1031, 120);
            this.statusCards.TabIndex = 0;
            // 
            // plcStatusCard
            // 
            this.plcStatusCard.BackColor = System.Drawing.Color.White;
            this.plcStatusCard.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.plcStatusCard.BorderWidth = 1F;
            this.plcStatusCard.Controls.Add(this.plcIndicator);
            this.plcStatusCard.Controls.Add(this.plcStatusLabel);
            this.plcStatusCard.Controls.Add(this.plcTitleLabel);
            this.plcStatusCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plcStatusCard.Location = new System.Drawing.Point(0, 10);
            this.plcStatusCard.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.plcStatusCard.Name = "plcStatusCard";
            this.plcStatusCard.Radius = 8;
            this.plcStatusCard.Shadow = 2;
            this.plcStatusCard.Size = new System.Drawing.Size(333, 100);
            this.plcStatusCard.TabIndex = 0;
            // 
            // plcIndicator
            // 
            this.plcIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.plcIndicator.Location = new System.Drawing.Point(285, 18);
            this.plcIndicator.Name = "plcIndicator";
            this.plcIndicator.Radius = 7;
            this.plcIndicator.Size = new System.Drawing.Size(14, 14);
            this.plcIndicator.TabIndex = 2;
            // 
            // plcStatusLabel
            // 
            this.plcStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10.5F);
            this.plcStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.plcStatusLabel.Location = new System.Drawing.Point(18, 50);
            this.plcStatusLabel.Name = "plcStatusLabel";
            this.plcStatusLabel.Size = new System.Drawing.Size(280, 25);
            this.plcStatusLabel.TabIndex = 1;
            this.plcStatusLabel.Text = "未连接";
            // 
            // plcTitleLabel
            // 
            this.plcTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.plcTitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.plcTitleLabel.Location = new System.Drawing.Point(18, 18);
            this.plcTitleLabel.Name = "plcTitleLabel";
            this.plcTitleLabel.Size = new System.Drawing.Size(250, 25);
            this.plcTitleLabel.TabIndex = 0;
            this.plcTitleLabel.Text = "PLC连接";
            // 
            // scannerStatusCard
            // 
            this.scannerStatusCard.BackColor = System.Drawing.Color.White;
            this.scannerStatusCard.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.scannerStatusCard.BorderWidth = 1F;
            this.scannerStatusCard.Controls.Add(this.scannerIndicator);
            this.scannerStatusCard.Controls.Add(this.scannerStatusLabel);
            this.scannerStatusCard.Controls.Add(this.scannerTitleLabel);
            this.scannerStatusCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scannerStatusCard.Location = new System.Drawing.Point(348, 10);
            this.scannerStatusCard.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.scannerStatusCard.Name = "scannerStatusCard";
            this.scannerStatusCard.Radius = 8;
            this.scannerStatusCard.Shadow = 2;
            this.scannerStatusCard.Size = new System.Drawing.Size(333, 100);
            this.scannerStatusCard.TabIndex = 1;
            // 
            // scannerIndicator
            // 
            this.scannerIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.scannerIndicator.Location = new System.Drawing.Point(285, 18);
            this.scannerIndicator.Name = "scannerIndicator";
            this.scannerIndicator.Radius = 7;
            this.scannerIndicator.Size = new System.Drawing.Size(14, 14);
            this.scannerIndicator.TabIndex = 2;
            // 
            // scannerStatusLabel
            // 
            this.scannerStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10.5F);
            this.scannerStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.scannerStatusLabel.Location = new System.Drawing.Point(18, 50);
            this.scannerStatusLabel.Name = "scannerStatusLabel";
            this.scannerStatusLabel.Size = new System.Drawing.Size(280, 25);
            this.scannerStatusLabel.TabIndex = 1;
            this.scannerStatusLabel.Text = "未连接";
            // 
            // scannerTitleLabel
            // 
            this.scannerTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.scannerTitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.scannerTitleLabel.Location = new System.Drawing.Point(18, 18);
            this.scannerTitleLabel.Name = "scannerTitleLabel";
            this.scannerTitleLabel.Size = new System.Drawing.Size(250, 25);
            this.scannerTitleLabel.TabIndex = 0;
            this.scannerTitleLabel.Text = "扫码枪";
            // 
            // screwStatusCard
            // 
            this.screwStatusCard.BackColor = System.Drawing.Color.White;
            this.screwStatusCard.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.screwStatusCard.BorderWidth = 1F;
            this.screwStatusCard.Controls.Add(this.tighteningAxisIndicator);
            this.screwStatusCard.Controls.Add(this.tighteningAxisStatusLabel);
            this.screwStatusCard.Controls.Add(this.screwTitleLabel);
            this.screwStatusCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.screwStatusCard.Location = new System.Drawing.Point(696, 10);
            this.screwStatusCard.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.screwStatusCard.Name = "screwStatusCard";
            this.screwStatusCard.Radius = 8;
            this.screwStatusCard.Shadow = 2;
            this.screwStatusCard.Size = new System.Drawing.Size(335, 100);
            this.screwStatusCard.TabIndex = 2;
            // 
            // tighteningAxisIndicator
            // 
            this.tighteningAxisIndicator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.tighteningAxisIndicator.Location = new System.Drawing.Point(285, 18);
            this.tighteningAxisIndicator.Name = "tighteningAxisIndicator";
            this.tighteningAxisIndicator.Radius = 7;
            this.tighteningAxisIndicator.Size = new System.Drawing.Size(14, 14);
            this.tighteningAxisIndicator.TabIndex = 2;
            // 
            // tighteningAxisStatusLabel
            // 
            this.tighteningAxisStatusLabel.Font = new System.Drawing.Font("微软雅黑", 10.5F);
            this.tighteningAxisStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(77)))), ((int)(((byte)(79)))));
            this.tighteningAxisStatusLabel.Location = new System.Drawing.Point(18, 50);
            this.tighteningAxisStatusLabel.Name = "tighteningAxisStatusLabel";
            this.tighteningAxisStatusLabel.Size = new System.Drawing.Size(280, 25);
            this.tighteningAxisStatusLabel.TabIndex = 1;
            this.tighteningAxisStatusLabel.Text = "未连接";
            // 
            // screwTitleLabel
            // 
            this.screwTitleLabel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.screwTitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.screwTitleLabel.Location = new System.Drawing.Point(18, 18);
            this.screwTitleLabel.Name = "screwTitleLabel";
            this.screwTitleLabel.Size = new System.Drawing.Size(250, 25);
            this.screwTitleLabel.TabIndex = 0;
            this.screwTitleLabel.Text = "拧紧轴";
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
            this.tighteningDataPanel.ResumeLayout(false);
            this.currentProductPanel.ResumeLayout(false);
            this.controlPanel.ResumeLayout(false);
            this.statusCards.ResumeLayout(false);
            this.plcStatusCard.ResumeLayout(false);
            this.scannerStatusCard.ResumeLayout(false);
            this.screwStatusCard.ResumeLayout(false);
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
        private System.Windows.Forms.TableLayoutPanel statusCards;
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
        private AntdUI.Panel tighteningDataPanel;
        private AntdUI.Label tighteningDataTitle;
        private AntdUI.Label lblTargetTorque;
        private AntdUI.Label lblRealtimeTorque;
        private AntdUI.Label lblCompletedTorque;
        private AntdUI.Label lblTorqueRange;
        private AntdUI.Divider torqueDivider;
        private AntdUI.Label lblRunningStatus;
        private AntdUI.Label lblLastProduct;
    }
}
