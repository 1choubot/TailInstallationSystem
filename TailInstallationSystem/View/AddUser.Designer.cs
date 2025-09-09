namespace TailInstallationSystem.View
{
    partial class AddUser
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
            this.panel1 = new AntdUI.Panel();
            this.btnAdd = new AntdUI.Button();
            this.txtConfirmPassword = new AntdUI.Input();
            this.txtPassword = new AntdUI.Input();
            this.txtUserName = new AntdUI.Input();
            this.label4 = new AntdUI.Label();
            this.label3 = new AntdUI.Label();
            this.label2 = new AntdUI.Label();
            this.label1 = new AntdUI.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Controls.Add(this.txtConfirmPassword);
            this.panel1.Controls.Add(this.txtPassword);
            this.panel1.Controls.Add(this.txtUserName);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(44, 26);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(946, 561);
            this.panel1.TabIndex = 1;
            this.panel1.Text = "panel1";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(387, 457);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(156, 67);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "添加";
            // 
            // txtConfirmPassword
            // 
            this.txtConfirmPassword.Location = new System.Drawing.Point(411, 362);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.Size = new System.Drawing.Size(222, 55);
            this.txtConfirmPassword.TabIndex = 2;
            this.txtConfirmPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(411, 243);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(222, 55);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(411, 138);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(222, 55);
            this.txtUserName.TabIndex = 0;
            this.txtUserName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(265, 382);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 23);
            this.label4.TabIndex = 1;
            this.label4.Text = "确认密码";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(265, 260);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 23);
            this.label3.TabIndex = 1;
            this.label3.Text = "密码";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(265, 156);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 23);
            this.label2.TabIndex = 1;
            this.label2.Text = "用户名";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(411, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = "添加用户";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // AddUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "AddUser";
            this.Size = new System.Drawing.Size(1043, 619);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Panel panel1;
        private AntdUI.Label label1;
        private AntdUI.Button btnAdd;
        private AntdUI.Input txtConfirmPassword;
        private AntdUI.Input txtPassword;
        private AntdUI.Input txtUserName;
        private AntdUI.Label label4;
        private AntdUI.Label label3;
        private AntdUI.Label label2;
    }
}
