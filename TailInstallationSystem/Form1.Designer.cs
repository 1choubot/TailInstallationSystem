namespace TailInstallationSystem
{
    partial class Form1
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            AntdUI.MenuItem menuItem1 = new AntdUI.MenuItem();
            AntdUI.MenuItem menuItem2 = new AntdUI.MenuItem();
            this.label1 = new AntdUI.Label();
            this.panel1 = new AntdUI.Panel();
            this.panelMain = new AntdUI.Panel();
            this.menu1 = new AntdUI.Menu();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1337, 51);
            this.label1.TabIndex = 0;
            this.label1.Text = "安装尾椎";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panelMain);
            this.panel1.Controls.Add(this.menu1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 51);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1337, 625);
            this.panel1.TabIndex = 1;
            this.panel1.Text = "panel1";
            // 
            // panelMain
            // 
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(196, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(1141, 625);
            this.panelMain.TabIndex = 3;
            this.panelMain.Text = "panel2";
            // 
            // menu1
            // 
            this.menu1.BackColor = System.Drawing.SystemColors.Control;
            this.menu1.Dock = System.Windows.Forms.DockStyle.Left;
            menuItem1.Name = "AddUser";
            menuItem1.Text = "添加用户";
            menuItem2.Name = "DeleteUser";
            menuItem2.Text = "删除用户";
            this.menu1.Items.Add(menuItem1);
            this.menu1.Items.Add(menuItem2);
            this.menu1.Location = new System.Drawing.Point(0, 0);
            this.menu1.Name = "menu1";
            this.menu1.Size = new System.Drawing.Size(196, 625);
            this.menu1.TabIndex = 2;
            this.menu1.Text = "menu1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1337, 676);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Label label1;
        private AntdUI.Panel panel1;
        private AntdUI.Panel panelMain;
        private AntdUI.Menu menu1;
    }
}

