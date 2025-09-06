using System;
using System.Linq;
using System.Windows.Forms;

namespace TailInstallationSystem.View
{
    public partial class AddUser : UserControl
    {
        public AddUser()
        {
            InitializeComponent();
            btnAdd.Click += BtnAdd_Click;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string username = txtUserName.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("用户名和密码不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("两次输入的密码不一致！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var db = new NodeInstrumentMESEntities())
            {
                if (db.Users.Any(u => u.UserName == username))
                {
                    MessageBox.Show("用户名已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var user = new Users
                {
                    UserName = username,
                    Password = password,
                    CreatedTime = DateTime.Now
                };
                db.Users.Add(user);
                db.SaveChanges();
                MessageBox.Show("用户添加成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtUserName.Text = "";
                txtPassword.Text = "";
                txtConfirmPassword.Text = "";
            }
        }
    }
}
