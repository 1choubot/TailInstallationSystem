namespace TailInstallationSystem
{
    partial class SettingsForm
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
            this.tabButtonPanel = new System.Windows.Forms.Panel();
            this.basicTabButton = new AntdUI.Button();
            this.networkTabButton = new AntdUI.Button();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.basicPanel = new System.Windows.Forms.Panel();
            this.logLevelComboBox = new AntdUI.Select();
            this.logLevelLabel = new System.Windows.Forms.Label();
            this.autoStartCheckBox = new AntdUI.Checkbox();
            this.networkPanel = new System.Windows.Forms.Panel();
            this.webSocketTextBox = new AntdUI.Input();
            this.webSocketLabel = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.cancelButton = new AntdUI.Button();
            this.okButton = new AntdUI.Button();
            this.tabButtonPanel.SuspendLayout();
            this.mainPanel.SuspendLayout();
            this.basicPanel.SuspendLayout();
            this.networkPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabButtonPanel
            // 
            this.tabButtonPanel.Controls.Add(this.networkTabButton);
            this.tabButtonPanel.Controls.Add(this.basicTabButton);
            this.tabButtonPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabButtonPanel.Location = new System.Drawing.Point(0, 0);
            this.tabButtonPanel.Name = "tabButtonPanel";
            this.tabButtonPanel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.tabButtonPanel.Size = new System.Drawing.Size(600, 50);
            this.tabButtonPanel.TabIndex = 0;
            // 
            // basicTabButton
            // 
            this.basicTabButton.Location = new System.Drawing.Point(20, 10);
            this.basicTabButton.Name = "basicTabButton";
            this.basicTabButton.Size = new System.Drawing.Size(100, 30);
            this.basicTabButton.TabIndex = 0;
            this.basicTabButton.Text = "基本设置";
            this.basicTabButton.Type = AntdUI.TTypeMini.Primary;
            this.basicTabButton.Click += new System.EventHandler(this.basicTabButton_Click);
            // 
            // networkTabButton
            // 
            this.networkTabButton.Location = new System.Drawing.Point(130, 10);
            this.networkTabButton.Name = "networkTabButton";
            this.networkTabButton.Size = new System.Drawing.Size(100, 30);
            this.networkTabButton.TabIndex = 1;
            this.networkTabButton.Text = "网络设置";
            this.networkTabButton.Click += new System.EventHandler(this.networkTabButton_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.networkPanel);
            this.mainPanel.Controls.Add(this.basicPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 50);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(600, 290);
            this.mainPanel.TabIndex = 1;
            // 
            // basicPanel
            // 
            this.basicPanel.Controls.Add(this.logLevelComboBox);
            this.basicPanel.Controls.Add(this.logLevelLabel);
            this.basicPanel.Controls.Add(this.autoStartCheckBox);
            this.basicPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.basicPanel.Location = new System.Drawing.Point(0, 0);
            this.basicPanel.Name = "basicPanel";
            this.basicPanel.Padding = new System.Windows.Forms.Padding(30);
            this.basicPanel.Size = new System.Drawing.Size(598, 288);
            this.basicPanel.TabIndex = 0;
            // 
            // logLevelComboBox
            // 
            this.logLevelComboBox.Location = new System.Drawing.Point(130, 70);
            this.logLevelComboBox.Name = "logLevelComboBox";
            this.logLevelComboBox.Size = new System.Drawing.Size(150, 35);
            this.logLevelComboBox.TabIndex = 2;
            // 
            // logLevelLabel
            // 
            this.logLevelLabel.AutoSize = true;
            this.logLevelLabel.Location = new System.Drawing.Point(30, 78);
            this.logLevelLabel.Name = "logLevelLabel";
            this.logLevelLabel.Size = new System.Drawing.Size(82, 20);
            this.logLevelLabel.TabIndex = 1;
            this.logLevelLabel.Text = "日志级别:";
            // 
            // autoStartCheckBox
            // 
            this.autoStartCheckBox.Location = new System.Drawing.Point(30, 30);
            this.autoStartCheckBox.Name = "autoStartCheckBox";
            this.autoStartCheckBox.Size = new System.Drawing.Size(120, 30);
            this.autoStartCheckBox.TabIndex = 0;
            this.autoStartCheckBox.Text = "开机自动启动";
            // 
            // networkPanel
            // 
            this.networkPanel.Controls.Add(this.webSocketTextBox);
            this.networkPanel.Controls.Add(this.webSocketLabel);
            this.networkPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.networkPanel.Location = new System.Drawing.Point(0, 0);
            this.networkPanel.Name = "networkPanel";
            this.networkPanel.Padding = new System.Windows.Forms.Padding(30);
            this.networkPanel.Size = new System.Drawing.Size(598, 288);
            this.networkPanel.TabIndex = 1;
            this.networkPanel.Visible = false;
            // 
            // webSocketTextBox
            // 
            this.webSocketTextBox.Location = new System.Drawing.Point(160, 28);
            this.webSocketTextBox.Name = "webSocketTextBox";
            this.webSocketTextBox.Size = new System.Drawing.Size(300, 35);
            this.webSocketTextBox.TabIndex = 1;
            // 
            // webSocketLabel
            // 
            this.webSocketLabel.AutoSize = true;
            this.webSocketLabel.Location = new System.Drawing.Point(30, 36);
            this.webSocketLabel.Name = "webSocketLabel";
            this.webSocketLabel.Size = new System.Drawing.Size(124, 20);
            this.webSocketLabel.TabIndex = 0;
            this.webSocketLabel.Text = "WebSocket服务器:";
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Controls.Add(this.okButton);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 340);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(600, 60);
            this.buttonPanel.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(500, 15);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 35);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "取消";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(410, 15);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 35);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "确定";
            this.okButton.Type = AntdUI.TTypeMini.Primary;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.tabButtonPanel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "系统设置";
            this.tabButtonPanel.ResumeLayout(false);
            this.mainPanel.ResumeLayout(false);
            this.basicPanel.ResumeLayout(false);
            this.basicPanel.PerformLayout();
            this.networkPanel.ResumeLayout(false);
            this.networkPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel tabButtonPanel;
        private AntdUI.Button basicTabButton;
        private AntdUI.Button networkTabButton;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel basicPanel;
        private System.Windows.Forms.Panel networkPanel;
        private AntdUI.Checkbox autoStartCheckBox;
        private System.Windows.Forms.Label logLevelLabel;
        private AntdUI.Select logLevelComboBox;
        private System.Windows.Forms.Label webSocketLabel;
        private AntdUI.Input webSocketTextBox;
        private System.Windows.Forms.Panel buttonPanel;
        private AntdUI.Button okButton;
        private AntdUI.Button cancelButton;
    }
}
