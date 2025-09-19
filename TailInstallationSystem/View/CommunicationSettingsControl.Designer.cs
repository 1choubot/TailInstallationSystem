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
            this.saveButton = new AntdUI.Button();
            this.pcGroupBox = new System.Windows.Forms.GroupBox();
            this.pcTestButton = new AntdUI.Button();
            this.pcPortTextBox = new AntdUI.Input();
            this.pcPortLabel = new AntdUI.Label();
            this.pcIpTextBox = new AntdUI.Input();
            this.pcIpLabel = new AntdUI.Label();
            this.screwGroupBox = new System.Windows.Forms.GroupBox();
            this.screwTestButton = new AntdUI.Button();
            this.screwPortTextBox = new AntdUI.Input();
            this.screwPortLabel = new AntdUI.Label();
            this.screwIpLabel = new AntdUI.Label();
            this.screwIpTextBox = new AntdUI.Input();
            this.scannerGroupBox = new System.Windows.Forms.GroupBox();
            this.scannerTestButton = new AntdUI.Button();
            this.scannerPortTextBox = new AntdUI.Input();
            this.scannerPortLabel = new AntdUI.Label();
            this.scannerIpTextBox = new AntdUI.Input();
            this.scannerIpLabel = new AntdUI.Label();
            this.plcGroupBox = new System.Windows.Forms.GroupBox();
            this.plcTestButton = new AntdUI.Button();
            this.plcStationTextBox = new AntdUI.Input();
            this.plcStationLabel = new AntdUI.Label();
            this.plcPortTextBox = new AntdUI.Input();
            this.plcPortLabel = new AntdUI.Label();
            this.plcIpTextBox = new AntdUI.Input();
            this.plcIpLabel = new AntdUI.Label();
            this.titleLabel = new AntdUI.Label();
            this.pcGroupBox.SuspendLayout();
            this.screwGroupBox.SuspendLayout();
            this.scannerGroupBox.SuspendLayout();
            this.plcGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(196)))), ((int)(((byte)(26)))));
            this.saveButton.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(560, 454);
            this.saveButton.Margin = new System.Windows.Forms.Padding(4);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(160, 44);
            this.saveButton.TabIndex = 17;
            this.saveButton.Text = "保存设置";
            this.saveButton.Type = AntdUI.TTypeMini.Success;
            // 
            // pcGroupBox
            // 
            this.pcGroupBox.Controls.Add(this.pcTestButton);
            this.pcGroupBox.Controls.Add(this.pcPortTextBox);
            this.pcGroupBox.Controls.Add(this.pcPortLabel);
            this.pcGroupBox.Controls.Add(this.pcIpTextBox);
            this.pcGroupBox.Controls.Add(this.pcIpLabel);
            this.pcGroupBox.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.pcGroupBox.Location = new System.Drawing.Point(653, 267);
            this.pcGroupBox.Margin = new System.Windows.Forms.Padding(4);
            this.pcGroupBox.Name = "pcGroupBox";
            this.pcGroupBox.Padding = new System.Windows.Forms.Padding(4);
            this.pcGroupBox.Size = new System.Drawing.Size(600, 162);
            this.pcGroupBox.TabIndex = 16;
            this.pcGroupBox.TabStop = false;
            this.pcGroupBox.Text = "PC通讯设置";
            // 
            // pcTestButton
            // 
            this.pcTestButton.Location = new System.Drawing.Point(440, 106);
            this.pcTestButton.Margin = new System.Windows.Forms.Padding(4);
            this.pcTestButton.Name = "pcTestButton";
            this.pcTestButton.Size = new System.Drawing.Size(133, 38);
            this.pcTestButton.TabIndex = 4;
            this.pcTestButton.Text = "测试连接";
            this.pcTestButton.Type = AntdUI.TTypeMini.Primary;
            // 
            // pcPortTextBox
            // 
            this.pcPortTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.pcPortTextBox.Location = new System.Drawing.Point(467, 31);
            this.pcPortTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.pcPortTextBox.Name = "pcPortTextBox";
            this.pcPortTextBox.Size = new System.Drawing.Size(107, 35);
            this.pcPortTextBox.TabIndex = 3;
            this.pcPortTextBox.Text = "8888";
            // 
            // pcPortLabel
            // 
            this.pcPortLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.pcPortLabel.Location = new System.Drawing.Point(373, 34);
            this.pcPortLabel.Margin = new System.Windows.Forms.Padding(4);
            this.pcPortLabel.Name = "pcPortLabel";
            this.pcPortLabel.Size = new System.Drawing.Size(80, 29);
            this.pcPortLabel.TabIndex = 2;
            this.pcPortLabel.Text = "端口:";
            // 
            // pcIpTextBox
            // 
            this.pcIpTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.pcIpTextBox.Location = new System.Drawing.Point(147, 31);
            this.pcIpTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.pcIpTextBox.Name = "pcIpTextBox";
            this.pcIpTextBox.Size = new System.Drawing.Size(200, 35);
            this.pcIpTextBox.TabIndex = 1;
            this.pcIpTextBox.Text = "192.168.1.102";
            // 
            // pcIpLabel
            // 
            this.pcIpLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.pcIpLabel.Location = new System.Drawing.Point(27, 34);
            this.pcIpLabel.Margin = new System.Windows.Forms.Padding(4);
            this.pcIpLabel.Name = "pcIpLabel";
            this.pcIpLabel.Size = new System.Drawing.Size(107, 29);
            this.pcIpLabel.TabIndex = 0;
            this.pcIpLabel.Text = "IP地址:";
            // 
            // screwGroupBox
            // 
            this.screwGroupBox.Controls.Add(this.screwTestButton);
            this.screwGroupBox.Controls.Add(this.screwPortTextBox);
            this.screwGroupBox.Controls.Add(this.screwPortLabel);
            this.screwGroupBox.Controls.Add(this.screwIpLabel);
            this.screwGroupBox.Controls.Add(this.screwIpTextBox);
            this.screwGroupBox.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.screwGroupBox.Location = new System.Drawing.Point(27, 267);
            this.screwGroupBox.Margin = new System.Windows.Forms.Padding(4);
            this.screwGroupBox.Name = "screwGroupBox";
            this.screwGroupBox.Padding = new System.Windows.Forms.Padding(4);
            this.screwGroupBox.Size = new System.Drawing.Size(600, 162);
            this.screwGroupBox.TabIndex = 15;
            this.screwGroupBox.TabStop = false;
            this.screwGroupBox.Text = "螺丝机连接设置";
            // 
            // screwTestButton
            // 
            this.screwTestButton.Location = new System.Drawing.Point(440, 106);
            this.screwTestButton.Margin = new System.Windows.Forms.Padding(4);
            this.screwTestButton.Name = "screwTestButton";
            this.screwTestButton.Size = new System.Drawing.Size(133, 38);
            this.screwTestButton.TabIndex = 4;
            this.screwTestButton.Text = "测试连接";
            this.screwTestButton.Type = AntdUI.TTypeMini.Primary;
            // 
            // screwPortTextBox
            // 
            this.screwPortTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.screwPortTextBox.Location = new System.Drawing.Point(466, 34);
            this.screwPortTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.screwPortTextBox.Name = "screwPortTextBox";
            this.screwPortTextBox.Size = new System.Drawing.Size(107, 35);
            this.screwPortTextBox.TabIndex = 3;
            this.screwPortTextBox.Text = "8888";
            // 
            // screwPortLabel
            // 
            this.screwPortLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this.screwPortLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.screwPortLabel.Location = new System.Drawing.Point(373, 43);
            this.screwPortLabel.Margin = new System.Windows.Forms.Padding(4);
            this.screwPortLabel.Name = "screwPortLabel";
            this.screwPortLabel.Size = new System.Drawing.Size(45, 20);
            this.screwPortLabel.TabIndex = 2;
            this.screwPortLabel.Text = "端口：";
            // 
            // screwIpLabel
            // 
            this.screwIpLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this.screwIpLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.screwIpLabel.Location = new System.Drawing.Point(27, 44);
            this.screwIpLabel.Margin = new System.Windows.Forms.Padding(4);
            this.screwIpLabel.Name = "screwIpLabel";
            this.screwIpLabel.Size = new System.Drawing.Size(59, 20);
            this.screwIpLabel.TabIndex = 0;
            this.screwIpLabel.Text = "IP地址：";
            // 
            // screwIpTextBox
            // 
            this.screwIpTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.screwIpTextBox.Location = new System.Drawing.Point(147, 34);
            this.screwIpTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.screwIpTextBox.Name = "screwIpTextBox";
            this.screwIpTextBox.Size = new System.Drawing.Size(200, 35);
            this.screwIpTextBox.TabIndex = 1;
            this.screwIpTextBox.Text = "192.168.1.102";
            // 
            // scannerGroupBox
            // 
            this.scannerGroupBox.Controls.Add(this.scannerTestButton);
            this.scannerGroupBox.Controls.Add(this.scannerPortTextBox);
            this.scannerGroupBox.Controls.Add(this.scannerPortLabel);
            this.scannerGroupBox.Controls.Add(this.scannerIpTextBox);
            this.scannerGroupBox.Controls.Add(this.scannerIpLabel);
            this.scannerGroupBox.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.scannerGroupBox.Location = new System.Drawing.Point(653, 91);
            this.scannerGroupBox.Margin = new System.Windows.Forms.Padding(4);
            this.scannerGroupBox.Name = "scannerGroupBox";
            this.scannerGroupBox.Padding = new System.Windows.Forms.Padding(4);
            this.scannerGroupBox.Size = new System.Drawing.Size(600, 162);
            this.scannerGroupBox.TabIndex = 14;
            this.scannerGroupBox.TabStop = false;
            this.scannerGroupBox.Text = "扫码枪连接设置";
            // 
            // scannerTestButton
            // 
            this.scannerTestButton.Location = new System.Drawing.Point(440, 106);
            this.scannerTestButton.Margin = new System.Windows.Forms.Padding(4);
            this.scannerTestButton.Name = "scannerTestButton";
            this.scannerTestButton.Size = new System.Drawing.Size(133, 38);
            this.scannerTestButton.TabIndex = 4;
            this.scannerTestButton.Text = "测试连接";
            this.scannerTestButton.Type = AntdUI.TTypeMini.Primary;
            // 
            // scannerPortTextBox
            // 
            this.scannerPortTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.scannerPortTextBox.Location = new System.Drawing.Point(467, 31);
            this.scannerPortTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.scannerPortTextBox.Name = "scannerPortTextBox";
            this.scannerPortTextBox.Size = new System.Drawing.Size(107, 35);
            this.scannerPortTextBox.TabIndex = 3;
            this.scannerPortTextBox.Text = "6666";
            // 
            // scannerPortLabel
            // 
            this.scannerPortLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.scannerPortLabel.Location = new System.Drawing.Point(373, 34);
            this.scannerPortLabel.Margin = new System.Windows.Forms.Padding(4);
            this.scannerPortLabel.Name = "scannerPortLabel";
            this.scannerPortLabel.Size = new System.Drawing.Size(80, 29);
            this.scannerPortLabel.TabIndex = 2;
            this.scannerPortLabel.Text = "端口:";
            // 
            // scannerIpTextBox
            // 
            this.scannerIpTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.scannerIpTextBox.Location = new System.Drawing.Point(147, 31);
            this.scannerIpTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.scannerIpTextBox.Name = "scannerIpTextBox";
            this.scannerIpTextBox.Size = new System.Drawing.Size(200, 35);
            this.scannerIpTextBox.TabIndex = 1;
            this.scannerIpTextBox.Text = "192.168.1.129";
            // 
            // scannerIpLabel
            // 
            this.scannerIpLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.scannerIpLabel.Location = new System.Drawing.Point(27, 34);
            this.scannerIpLabel.Margin = new System.Windows.Forms.Padding(4);
            this.scannerIpLabel.Name = "scannerIpLabel";
            this.scannerIpLabel.Size = new System.Drawing.Size(107, 29);
            this.scannerIpLabel.TabIndex = 0;
            this.scannerIpLabel.Text = "IP地址:";
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
            this.plcGroupBox.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.plcGroupBox.Location = new System.Drawing.Point(27, 91);
            this.plcGroupBox.Margin = new System.Windows.Forms.Padding(4);
            this.plcGroupBox.Name = "plcGroupBox";
            this.plcGroupBox.Padding = new System.Windows.Forms.Padding(4);
            this.plcGroupBox.Size = new System.Drawing.Size(600, 162);
            this.plcGroupBox.TabIndex = 13;
            this.plcGroupBox.TabStop = false;
            this.plcGroupBox.Text = "PLC连接设置";
            // 
            // plcTestButton
            // 
            this.plcTestButton.Location = new System.Drawing.Point(440, 106);
            this.plcTestButton.Margin = new System.Windows.Forms.Padding(4);
            this.plcTestButton.Name = "plcTestButton";
            this.plcTestButton.Size = new System.Drawing.Size(133, 38);
            this.plcTestButton.TabIndex = 6;
            this.plcTestButton.Text = "测试连接";
            this.plcTestButton.Type = AntdUI.TTypeMini.Primary;
            // 
            // plcStationTextBox
            // 
            this.plcStationTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcStationTextBox.Location = new System.Drawing.Point(147, 75);
            this.plcStationTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.plcStationTextBox.Name = "plcStationTextBox";
            this.plcStationTextBox.Size = new System.Drawing.Size(133, 35);
            this.plcStationTextBox.TabIndex = 5;
            this.plcStationTextBox.Text = "1";
            // 
            // plcStationLabel
            // 
            this.plcStationLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcStationLabel.Location = new System.Drawing.Point(27, 78);
            this.plcStationLabel.Margin = new System.Windows.Forms.Padding(4);
            this.plcStationLabel.Name = "plcStationLabel";
            this.plcStationLabel.Size = new System.Drawing.Size(107, 29);
            this.plcStationLabel.TabIndex = 4;
            this.plcStationLabel.Text = "站号:";
            // 
            // plcPortTextBox
            // 
            this.plcPortTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcPortTextBox.Location = new System.Drawing.Point(467, 31);
            this.plcPortTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.plcPortTextBox.Name = "plcPortTextBox";
            this.plcPortTextBox.Size = new System.Drawing.Size(107, 35);
            this.plcPortTextBox.TabIndex = 3;
            this.plcPortTextBox.Text = "502";
            // 
            // plcPortLabel
            // 
            this.plcPortLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcPortLabel.Location = new System.Drawing.Point(373, 34);
            this.plcPortLabel.Margin = new System.Windows.Forms.Padding(4);
            this.plcPortLabel.Name = "plcPortLabel";
            this.plcPortLabel.Size = new System.Drawing.Size(80, 29);
            this.plcPortLabel.TabIndex = 2;
            this.plcPortLabel.Text = "端口:";
            // 
            // plcIpTextBox
            // 
            this.plcIpTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcIpTextBox.Location = new System.Drawing.Point(147, 31);
            this.plcIpTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.plcIpTextBox.Name = "plcIpTextBox";
            this.plcIpTextBox.Size = new System.Drawing.Size(200, 35);
            this.plcIpTextBox.TabIndex = 1;
            this.plcIpTextBox.Text = "192.168.1.88";
            // 
            // plcIpLabel
            // 
            this.plcIpLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.plcIpLabel.Location = new System.Drawing.Point(27, 34);
            this.plcIpLabel.Margin = new System.Windows.Forms.Padding(4);
            this.plcIpLabel.Name = "plcIpLabel";
            this.plcIpLabel.Size = new System.Drawing.Size(107, 29);
            this.plcIpLabel.TabIndex = 0;
            this.plcIpLabel.Text = "IP地址:";
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(27, 41);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(4);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(267, 38);
            this.titleLabel.TabIndex = 12;
            this.titleLabel.Text = "通讯设置";
            // 
            // CommunicationSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.pcGroupBox);
            this.Controls.Add(this.screwGroupBox);
            this.Controls.Add(this.scannerGroupBox);
            this.Controls.Add(this.plcGroupBox);
            this.Controls.Add(this.titleLabel);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "CommunicationSettingsControl";
            this.Padding = new System.Windows.Forms.Padding(13, 12, 13, 25);
            this.Size = new System.Drawing.Size(1280, 538);
            this.pcGroupBox.ResumeLayout(false);
            this.screwGroupBox.ResumeLayout(false);
            this.screwGroupBox.PerformLayout();
            this.scannerGroupBox.ResumeLayout(false);
            this.plcGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Button saveButton;
        private System.Windows.Forms.GroupBox pcGroupBox;
        private AntdUI.Button pcTestButton;
        private AntdUI.Input pcPortTextBox;
        private AntdUI.Label pcPortLabel;
        private AntdUI.Input pcIpTextBox;
        private AntdUI.Label pcIpLabel;
        private System.Windows.Forms.GroupBox screwGroupBox;
        private AntdUI.Button screwTestButton;
        private AntdUI.Input screwPortTextBox;
        private AntdUI.Label screwPortLabel;
        private AntdUI.Label screwIpLabel;
        private AntdUI.Input screwIpTextBox;
        private System.Windows.Forms.GroupBox scannerGroupBox;
        private AntdUI.Button scannerTestButton;
        private AntdUI.Input scannerPortTextBox;
        private AntdUI.Label scannerPortLabel;
        private AntdUI.Input scannerIpTextBox;
        private AntdUI.Label scannerIpLabel;
        private System.Windows.Forms.GroupBox plcGroupBox;
        private AntdUI.Button plcTestButton;
        private AntdUI.Input plcStationTextBox;
        private AntdUI.Label plcStationLabel;
        private AntdUI.Input plcPortTextBox;
        private AntdUI.Label plcPortLabel;
        private AntdUI.Input plcIpTextBox;
        private AntdUI.Label plcIpLabel;
        private AntdUI.Label titleLabel;
    }
}
