namespace TailInstallationSystem
{
    partial class CommunicationSettingsControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new AntdUI.Label();
            this.plcGroupBox = new System.Windows.Forms.GroupBox();
            this.plcTestButton = new AntdUI.Button();
            this.plcStationTextBox = new AntdUI.Input();
            this.plcStationLabel = new AntdUI.Label();
            this.plcPortTextBox = new AntdUI.Input();
            this.plcPortLabel = new AntdUI.Label();
            this.plcIpTextBox = new AntdUI.Input();
            this.plcIpLabel = new AntdUI.Label();
            this.scannerGroupBox = new System.Windows.Forms.GroupBox();
            this.scannerTestButton = new AntdUI.Button();
            this.scannerPortTextBox = new AntdUI.Input();
            this.scannerPortLabel = new AntdUI.Label();
            this.scannerIpTextBox = new AntdUI.Input();
            this.scannerIpLabel = new AntdUI.Label();
            this.screwGroupBox = new System.Windows.Forms.GroupBox();
            this.screwTestButton = new AntdUI.Button();
            this.screwBaudComboBox = new AntdUI.Select();
            this.screwBaudLabel = new AntdUI.Label();
            this.screwComComboBox = new AntdUI.Select();
            this.screwComLabel = new AntdUI.Label();
            this.pcGroupBox = new System.Windows.Forms.GroupBox();
            this.pcTestButton = new AntdUI.Button();
            this.pcPortTextBox = new AntdUI.Input();
            this.pcPortLabel = new AntdUI.Label();
            this.pcIpTextBox = new AntdUI.Input();
            this.pcIpLabel = new AntdUI.Label();
            this.saveButton = new AntdUI.Button();
            this.plcGroupBox.SuspendLayout();
            this.scannerGroupBox.SuspendLayout();
            this.screwGroupBox.SuspendLayout();
            this.pcGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(20, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 40);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "通讯设置";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // plcGroupBox
            // 
            this.plcGroupBox.Controls.Add(this.plcTestButton);
            this.plcGroupBox.Controls.Add(this.plcStationTextBox);
            this.plcGroupBox.Controls.Add(this.plcStationLabel);
            this.plcGroupBox.Controls.Add(this.plcPortTextBox);
            this.plcGroupBox.Controls.Add(this.plcPortLabel);
            this.plcGroupBox.Controls.Add(this.plcIpTextBox);
            this.plcGroupBox.Controls.Add(this.plcIpLabel);
            this.plcGroupBox.Font = new System.Drawing.Font("Microsoft YaHei", 12F, System.Drawing.FontStyle.Bold);
            this.plcGroupBox.Location = new System.Drawing.Point(20, 80);
            this.plcGroupBox.Name = "plcGroupBox";
            this.plcGroupBox.Size = new System.Drawing.Size(450, 150);
            this.plcGroupBox.TabIndex = 1;
            this.plcGroupBox.TabStop = false;
            this.plcGroupBox.Text = "PLC连接设置";
            // 
            // plcTestButton
            // 
            this.plcTestButton.Location = new System.Drawing.Point(330, 100);
            this.plcTestButton.Name = "plcTestButton";
            this.plcTestButton.Radius = 6;
            this.plcTestButton.Size = new System.Drawing.Size(100, 35);
            this.plcTestButton.TabIndex = 6;
            this.plcTestButton.Text = "测试连接";
            this.plcTestButton.Type = AntdUI.TTypeMini.Primary;
            this.plcTestButton.Click += new System.EventHandler(this.plcTestButton_Click);
            // 
            // plcStationTextBox
            // 
            this.plcStationTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcStationTextBox.Location = new System.Drawing.Point(110, 68);
            this.plcStationTextBox.Name = "plcStationTextBox";
            this.plcStationTextBox.Size = new System.Drawing.Size(100, 30);
            this.plcStationTextBox.TabIndex = 5;
            this.plcStationTextBox.Text = "1";
            // 
            // plcStationLabel
            // 
            this.plcStationLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcStationLabel.Location = new System.Drawing.Point(20, 70);
            this.plcStationLabel.Name = "plcStationLabel";
            this.plcStationLabel.Size = new System.Drawing.Size(80, 25);
            this.plcStationLabel.TabIndex = 4;
            this.plcStationLabel.Text = "站号:";
            this.plcStationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // plcPortTextBox
            // 
            this.plcPortTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcPortTextBox.Location = new System.Drawing.Point(350, 28);
            this.plcPortTextBox.Name = "plcPortTextBox";
            this.plcPortTextBox.Size = new System.Drawing.Size(80, 30);
            this.plcPortTextBox.TabIndex = 3;
            this.plcPortTextBox.Text = "502";
            // 
            // plcPortLabel
            // 
            this.plcPortLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcPortLabel.Location = new System.Drawing.Point(280, 30);
            this.plcPortLabel.Name = "plcPortLabel";
            this.plcPortLabel.Size = new System.Drawing.Size(60, 25);
            this.plcPortLabel.TabIndex = 2;
            this.plcPortLabel.Text = "端口:";
            this.plcPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // plcIpTextBox
            // 
            this.plcIpTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcIpTextBox.Location = new System.Drawing.Point(110, 28);
            this.plcIpTextBox.Name = "plcIpTextBox";
            this.plcIpTextBox.Size = new System.Drawing.Size(150, 30);
            this.plcIpTextBox.TabIndex = 1;
            this.plcIpTextBox.Text = "192.168.1.88";
            // 
            // plcIpLabel
            // 
            this.plcIpLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.plcIpLabel.Location = new System.Drawing.Point(20, 30);
            this.plcIpLabel.Name = "plcIpLabel";
            this.plcIpLabel.Size = new System.Drawing.Size(80, 25);
            this.plcIpLabel.TabIndex = 0;
            this.plcIpLabel.Text = "IP地址:";
            this.plcIpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // scannerGroupBox
            // 
            this.scannerGroupBox.Controls.Add(this.scannerTestButton);
            this.scannerGroupBox.Controls.Add(this.scannerPortTextBox);
            this.scannerGroupBox.Controls.Add(this.scannerPortLabel);
            this.scannerGroupBox.Controls.Add(this.scannerIpTextBox);
            this.scannerGroupBox.Controls.Add(this.scannerIpLabel);
            this.scannerGroupBox.Font = new System.Drawing.Font("Microsoft YaHei", 12F, System.Drawing.FontStyle.Bold);
            this.scannerGroupBox.Location = new System.Drawing.Point(490, 80);
            this.scannerGroupBox.Name = "scannerGroupBox";
            this.scannerGroupBox.Size = new System.Drawing.Size(450, 150);
            this.scannerGroupBox.TabIndex = 2;
            this.scannerGroupBox.TabStop = false;
            this.scannerGroupBox.Text = "扫码枪连接设置";
            // 
            // scannerTestButton
            // 
            this.scannerTestButton.Location = new System.Drawing.Point(330, 100);
            this.scannerTestButton.Name = "scannerTestButton";
            this.scannerTestButton.Radius = 6;
            this.scannerTestButton.Size = new System.Drawing.Size(100, 35);
            this.scannerTestButton.TabIndex = 4;
            this.scannerTestButton.Text = "测试连接";
            this.scannerTestButton.Type = AntdUI.TTypeMini.Primary;
            this.scannerTestButton.Click += new System.EventHandler(this.scannerTestButton_Click);
            // 
            // scannerPortTextBox
            // 
            this.scannerPortTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.scannerPortTextBox.Location = new System.Drawing.Point(350, 28);
            this.scannerPortTextBox.Name = "scannerPortTextBox";
            this.scannerPortTextBox.Size = new System.Drawing.Size(80, 30);
            this.scannerPortTextBox.TabIndex = 3;
            this.scannerPortTextBox.Text = "6666";
            // 
            // scannerPortLabel
            // 
            this.scannerPortLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.scannerPortLabel.Location = new System.Drawing.Point(280, 30);
            this.scannerPortLabel.Name = "scannerPortLabel";
            this.scannerPortLabel.Size = new System.Drawing.Size(60, 25);
            this.scannerPortLabel.TabIndex = 2;
            this.scannerPortLabel.Text = "端口:";
            this.scannerPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // scannerIpTextBox
            // 
            this.scannerIpTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.scannerIpTextBox.Location = new System.Drawing.Point(110, 28);
            this.scannerIpTextBox.Name = "scannerIpTextBox";
            this.scannerIpTextBox.Size = new System.Drawing.Size(150, 30);
            this.scannerIpTextBox.TabIndex = 1;
            this.scannerIpTextBox.Text = "192.168.1.129";
            // 
            // scannerIpLabel
            // 
            this.scannerIpLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.scannerIpLabel.Location = new System.Drawing.Point(20, 30);
            this.scannerIpLabel.Name = "scannerIpLabel";
            this.scannerIpLabel.Size = new System.Drawing.Size(80, 25);
            this.scannerIpLabel.TabIndex = 0;
            this.scannerIpLabel.Text = "IP地址:";
            this.scannerIpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // screwGroupBox
            // 
            this.screwGroupBox.Controls.Add(this.screwTestButton);
            this.screwGroupBox.Controls.Add(this.screwBaudComboBox);
            this.screwGroupBox.Controls.Add(this.screwBaudLabel);
            this.screwGroupBox.Controls.Add(this.screwComComboBox);
            this.screwGroupBox.Controls.Add(this.screwComLabel);
            this.screwGroupBox.Font = new System.Drawing.Font("Microsoft YaHei", 12F, System.Drawing.FontStyle.Bold);
            this.screwGroupBox.Location = new System.Drawing.Point(20, 250);
            this.screwGroupBox.Name = "screwGroupBox";
            this.screwGroupBox.Size = new System.Drawing.Size(450, 150);
            this.screwGroupBox.TabIndex = 3;
            this.screwGroupBox.TabStop = false;
            this.screwGroupBox.Text = "螺丝机连接设置";
            // 
            // screwTestButton
            // 
            this.screwTestButton.Location = new System.Drawing.Point(330, 100);
            this.screwTestButton.Name = "screwTestButton";
            this.screwTestButton.Radius = 6;
            this.screwTestButton.Size = new System.Drawing.Size(100, 35);
            this.screwTestButton.TabIndex = 4;
            this.screwTestButton.Text = "测试连接";
            this.screwTestButton.Type = AntdUI.TTypeMini.Primary;
            this.screwTestButton.Click += new System.EventHandler(this.screwTestButton_Click);
            // 
            // screwBaudComboBox
            // 
            this.screwBaudComboBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.screwBaudComboBox.Location = new System.Drawing.Point(330, 28);
            this.screwBaudComboBox.Name = "screwBaudComboBox";
            this.screwBaudComboBox.Size = new System.Drawing.Size(100, 30);
            this.screwBaudComboBox.TabIndex = 3;
            this.screwBaudComboBox.Text = "9600";
            // 
            // screwBaudLabel
            // 
            this.screwBaudLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.screwBaudLabel.Location = new System.Drawing.Point(250, 30);
            this.screwBaudLabel.Name = "screwBaudLabel";
            this.screwBaudLabel.Size = new System.Drawing.Size(70, 25);
            this.screwBaudLabel.TabIndex = 2;
            this.screwBaudLabel.Text = "波特率:";
            this.screwBaudLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // screwComComboBox
            // 
            this.screwComComboBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.screwComComboBox.Location = new System.Drawing.Point(110, 28);
            this.screwComComboBox.Name = "screwComComboBox";
            this.screwComComboBox.Size = new System.Drawing.Size(120, 30);
            this.screwComComboBox.TabIndex = 1;
            this.screwComComboBox.Text = "COM1";
            // 
            // screwComLabel
            // 
            this.screwComLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.screwComLabel.Location = new System.Drawing.Point(20, 30);
            this.screwComLabel.Name = "screwComLabel";
            this.screwComLabel.Size = new System.Drawing.Size(80, 25);
            this.screwComLabel.TabIndex = 0;
            this.screwComLabel.Text = "串口:";
            this.screwComLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pcGroupBox
            // 
            this.pcGroupBox.Controls.Add(this.pcTestButton);
            this.pcGroupBox.Controls.Add(this.pcPortTextBox);
            this.pcGroupBox.Controls.Add(this.pcPortLabel);
            this.pcGroupBox.Controls.Add(this.pcIpTextBox);
            this.pcGroupBox.Controls.Add(this.pcIpLabel);
            this.pcGroupBox.Font = new System.Drawing.Font("Microsoft YaHei", 12F, System.Drawing.FontStyle.Bold);
            this.pcGroupBox.Location = new System.Drawing.Point(490, 250);
            this.pcGroupBox.Name = "pcGroupBox";
            this.pcGroupBox.Size = new System.Drawing.Size(450, 150);
            this.pcGroupBox.TabIndex = 4;
            this.pcGroupBox.TabStop = false;
            this.pcGroupBox.Text = "PC通讯设置";
            // 
            // pcTestButton
            // 
            this.pcTestButton.Location = new System.Drawing.Point(330, 100);
            this.pcTestButton.Name = "pcTestButton";
            this.pcTestButton.Radius = 6;
            this.pcTestButton.Size = new System.Drawing.Size(100, 35);
            this.pcTestButton.TabIndex = 4;
            this.pcTestButton.Text = "测试连接";
            this.pcTestButton.Type = AntdUI.TTypeMini.Primary;
            this.pcTestButton.Click += new System.EventHandler(this.pcTestButton_Click);
            // 
            // pcPortTextBox
            // 
            this.pcPortTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.pcPortTextBox.Location = new System.Drawing.Point(350, 28);
            this.pcPortTextBox.Name = "pcPortTextBox";
            this.pcPortTextBox.Size = new System.Drawing.Size(80, 30);
            this.pcPortTextBox.TabIndex = 3;
            this.pcPortTextBox.Text = "8888";
            // 
            // pcPortLabel
            // 
            this.pcPortLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.pcPortLabel.Location = new System.Drawing.Point(280, 30);
            this.pcPortLabel.Name = "pcPortLabel";
            this.pcPortLabel.Size = new System.Drawing.Size(60, 25);
            this.pcPortLabel.TabIndex = 2;
            this.pcPortLabel.Text = "端口:";
            this.pcPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pcIpTextBox
            // 
            this.pcIpTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.pcIpTextBox.Location = new System.Drawing.Point(110, 28);
            this.pcIpTextBox.Name = "pcIpTextBox";
            this.pcIpTextBox.Size = new System.Drawing.Size(150, 30);
            this.pcIpTextBox.TabIndex = 1;
            this.pcIpTextBox.Text = "192.168.1.102";
            // 
            // pcIpLabel
            // 
            this.pcIpLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.pcIpLabel.Location = new System.Drawing.Point(20, 30);
            this.pcIpLabel.Name = "pcIpLabel";
            this.pcIpLabel.Size = new System.Drawing.Size(80, 25);
            this.pcIpLabel.TabIndex = 0;
            this.pcIpLabel.Text = "IP地址:";
            this.pcIpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.saveButton.Font = new System.Drawing.Font("Microsoft YaHei", 10F, System.Drawing.FontStyle.Bold);
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(420, 430);
            this.saveButton.Name = "saveButton";
            this.saveButton.Radius = 6;
            this.saveButton.Size = new System.Drawing.Size(120, 40);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "保存设置";
            this.saveButton.Type = AntdUI.TTypeMini.Success;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // CommunicationSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.pcGroupBox);
            this.Controls.Add(this.screwGroupBox);
            this.Controls.Add(this.scannerGroupBox);
            this.Controls.Add(this.plcGroupBox);
            this.Controls.Add(this.titleLabel);
            this.Name = "CommunicationSettingsControl";
            this.Padding = new System.Windows.Forms.Padding(20);
            this.Size = new System.Drawing.Size(960, 500);
            this.plcGroupBox.ResumeLayout(false);
            this.scannerGroupBox.ResumeLayout(false);
            this.screwGroupBox.ResumeLayout(false);
            this.pcGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Label titleLabel;
        private System.Windows.Forms.GroupBox plcGroupBox;
        private AntdUI.Button plcTestButton;
        private AntdUI.Input plcStationTextBox;
        private AntdUI.Label plcStationLabel;
        private AntdUI.Input plcPortTextBox;
        private AntdUI.Label plcPortLabel;
        private AntdUI.Input plcIpTextBox;
        private AntdUI.Label plcIpLabel;
        private System.Windows.Forms.GroupBox scannerGroupBox;
        private AntdUI.Button scannerTestButton;
        private AntdUI.Input scannerPortTextBox;
        private AntdUI.Label scannerPortLabel;
        private AntdUI.Input scannerIpTextBox;
        private AntdUI.Label scannerIpLabel;
        private System.Windows.Forms.GroupBox screwGroupBox;
        private AntdUI.Button screwTestButton;
        private AntdUI.Select screwBaudComboBox;
        private AntdUI.Label screwBaudLabel;
        private AntdUI.Select screwComComboBox;
        private AntdUI.Label screwComLabel;
        private System.Windows.Forms.GroupBox pcGroupBox;
        private AntdUI.Button pcTestButton;
        private AntdUI.Input pcPortTextBox;
        private AntdUI.Label pcPortLabel;
        private AntdUI.Input pcIpTextBox;
        private AntdUI.Label pcIpLabel;
        private AntdUI.Button saveButton;
    }
}