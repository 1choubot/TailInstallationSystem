using System;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    public partial class UserAddDialog : Form
    {
        private AntdUI.Panel panel1;
        private AntdUI.Button btnCancel;
        private AntdUI.Button btnOK;
        private AntdUI.Label label2;
        private AntdUI.Label label3;
        private AntdUI.Label label1;
        private AntdUI.Input txtUserName;
        private AntdUI.Input txtPassword;

        public string UserName => txtUserName.Text.Trim();
        public string Password => txtPassword.Text;
        public DialogResult Result { get; private set; }

        public UserAddDialog()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                MessageBox.Show("用户名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("密码不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result = DialogResult.OK;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Result = DialogResult.Cancel;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.panel1 = new AntdUI.Panel();
            this.btnCancel = new AntdUI.Button();
            this.btnOK = new AntdUI.Button();
            this.label2 = new AntdUI.Label();
            this.label3 = new AntdUI.Label();
            this.label1 = new AntdUI.Label();
            this.txtUserName = new AntdUI.Input();
            this.txtPassword = new AntdUI.Input();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOK);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.txtUserName);
            this.panel1.Controls.Add(this.txtPassword);
            this.panel1.Location = new System.Drawing.Point(40, 20);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(520, 250);
            this.panel1.TabIndex = 0;
            this.panel1.Text = "panel1";
            // 
            // btnCancel - 取消按钮 
            // 
            this.btnCancel.Type = AntdUI.TTypeMini.Default;          
            this.btnCancel.DefaultBack = System.Drawing.Color.LightGray;      
            this.btnCancel.DefaultBorderColor = System.Drawing.Color.Gray;    
            this.btnCancel.ForeColor = System.Drawing.Color.Black;         
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.btnCancel.Location = new System.Drawing.Point(270, 180);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 36);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK - 确认按钮
            // 
            this.btnOK.Type = AntdUI.TTypeMini.Primary;              
            this.btnOK.BackColor = System.Drawing.Color.DodgerBlue;         
            this.btnOK.ForeColor = System.Drawing.Color.White;             
            this.btnOK.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.btnOK.Location = new System.Drawing.Point(144, 180);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(106, 36);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "确认";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.label2.Location = new System.Drawing.Point(120, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 28);
            this.label2.TabIndex = 1;
            this.label2.Text = "密码";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(0, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(520, 32);
            this.label3.TabIndex = 1;
            this.label3.Text = "添加用户";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.label1.Location = new System.Drawing.Point(120, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 28);
            this.label1.TabIndex = 1;
            this.label1.Text = "用户名";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(220, 75);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(180, 32);
            this.txtUserName.TabIndex = 0;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(220, 125);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(180, 32);
            this.txtPassword.TabIndex = 0;
            // 
            // UserAddDialog
            // 
            this.ClientSize = new System.Drawing.Size(600, 290);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "UserAddDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "添加用户";                                     
            this.MaximizeBox = false;                                 
            this.MinimizeBox = false;                                  
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}