namespace TailInstallationSystem
{
    partial class SystemLogControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.toolbarPanel = new System.Windows.Forms.Panel();
            this.refreshButton = new AntdUI.Button();
            this.clearButton = new AntdUI.Button();
            this.exportButton = new AntdUI.Button();
            this.autoRefreshCheckBox = new System.Windows.Forms.CheckBox();
            this.logDisplayTextBox = new System.Windows.Forms.RichTextBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.toolbarPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(20, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 40);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "系统日志";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.toolbarPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.toolbarPanel.Controls.Add(this.autoRefreshCheckBox);
            this.toolbarPanel.Controls.Add(this.exportButton);
            this.toolbarPanel.Controls.Add(this.clearButton);
            this.toolbarPanel.Controls.Add(this.refreshButton);
            this.toolbarPanel.Location = new System.Drawing.Point(20, 70);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Size = new System.Drawing.Size(920, 50);
            this.toolbarPanel.TabIndex = 1;
            // 
            // refreshButton
            // 
            this.refreshButton.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.refreshButton.Location = new System.Drawing.Point(10, 8);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(80, 35);
            this.refreshButton.TabIndex = 0;
            this.refreshButton.Text = "🔄 刷新";
            this.refreshButton.Type = AntdUI.TTypeMini.Primary;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // clearButton
            // 
            this.clearButton.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.clearButton.Location = new System.Drawing.Point(100, 8);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(80, 35);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "🗑️ 清空";
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // exportButton
            // 
            this.exportButton.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.exportButton.Location = new System.Drawing.Point(190, 8);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(80, 35);
            this.exportButton.TabIndex = 2;
            this.exportButton.Text = "📤 导出";
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            // 
            // autoRefreshCheckBox
            // 
            this.autoRefreshCheckBox.AutoSize = true;
            this.autoRefreshCheckBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.autoRefreshCheckBox.Location = new System.Drawing.Point(290, 15);
            this.autoRefreshCheckBox.Name = "autoRefreshCheckBox";
            this.autoRefreshCheckBox.Size = new System.Drawing.Size(75, 21);
            this.autoRefreshCheckBox.TabIndex = 3;
            this.autoRefreshCheckBox.Text = "自动刷新";
            this.autoRefreshCheckBox.UseVisualStyleBackColor = true;
            this.autoRefreshCheckBox.CheckedChanged += new System.EventHandler(this.autoRefreshCheckBox_CheckedChanged);
            // 
            // logDisplayTextBox
            // 
            this.logDisplayTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.logDisplayTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logDisplayTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.logDisplayTextBox.Location = new System.Drawing.Point(20, 130);
            this.logDisplayTextBox.Name = "logDisplayTextBox";
            this.logDisplayTextBox.ReadOnly = true;
            this.logDisplayTextBox.Size = new System.Drawing.Size(920, 450);
            this.logDisplayTextBox.TabIndex = 2;
            this.logDisplayTextBox.Text = "";
            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
            this.statusLabel.ForeColor = System.Drawing.Color.Gray;
            this.statusLabel.Location = new System.Drawing.Point(20, 590);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(920, 20);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "就绪";
            // 
            // SystemLogControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.logDisplayTextBox);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.titleLabel);
            this.Name = "SystemLogControl";
            this.Padding = new System.Windows.Forms.Padding(20);
            this.Size = new System.Drawing.Size(980, 640);
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Panel toolbarPanel;
        private AntdUI.Button refreshButton;
        private AntdUI.Button clearButton;
        private AntdUI.Button exportButton;
        private System.Windows.Forms.CheckBox autoRefreshCheckBox;
        private System.Windows.Forms.RichTextBox logDisplayTextBox;
        private System.Windows.Forms.Label statusLabel;
    }
}
