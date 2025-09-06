using System;
using System.Linq;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true; // 设置为密码模式
            btnLogin.Click += BtnLogin_Click;
            this.Load += LoginForm_Load;
        }

        // 启动时自动填充
        private void LoginForm_Load(object sender, EventArgs e)
        {
            txtUserName.Text = Properties.Settings.Default.UserName;
            txtPassword.Text = Properties.Settings.Default.Password;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUserName.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var db = new NodeInstrumentMESEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.UserName == username && u.Password == password);
                if (user != null)
                {
                    // 登录成功，保存账号密码
                    Properties.Settings.Default.UserName = username;
                    Properties.Settings.Default.Password = password;
                    Properties.Settings.Default.Save();

                    MessageBox.Show("登录成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Form1 mainForm = new Form1();
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("用户名或密码错误！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
