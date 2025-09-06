using System;
using System.Linq;
using System.Windows.Forms;

namespace TailInstallationSystem.View
{
    public partial class DeleteUser : UserControl
    {
        public DeleteUser()
        {
            InitializeComponent();
            dgvUsers.CellClick += dgvUsers_CellClick;
            btnSearch.Click += btnSearch_Click; // 绑定搜索按钮事件
            LoadUserList();
        }

        // 搜索按钮点击事件
        private void btnSearch_Click(object sender, EventArgs e)
        {
            LoadUserList(txtSearch.Text.Trim());
        }

        // 加载用户列表
        private void LoadUserList(string keyword = "")
        {
            dgvUsers.Rows.Clear();

            using (var db = new NodeInstrumentMESEntities())
            {
                var query = db.Users.AsQueryable();
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.UserName.Contains(keyword));
                }

                foreach (var user in query)
                {
                    int rowIndex = dgvUsers.Rows.Add();
                    dgvUsers.Rows[rowIndex].Cells["UserName"].Value = user.UserName;
                    dgvUsers.Rows[rowIndex].Cells["CreatedTime"].Value = user.CreatedTime?.ToString("yyyy-MM-dd HH:mm:ss");
                    dgvUsers.Rows[rowIndex].Tag = user.Id; // 用于删除时定位
                }
            }
        }

        // 删除按钮点击事件
        private void dgvUsers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvUsers.Columns[e.ColumnIndex].Name == "colDelete")
            {
                var userName = dgvUsers.Rows[e.RowIndex].Cells["UserName"].Value?.ToString();
                var userId = (int)dgvUsers.Rows[e.RowIndex].Tag;

                var result = MessageBox.Show($"确定要删除用户 {userName} 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    using (var db = new NodeInstrumentMESEntities())
                    {
                        var user = db.Users.Find(userId);
                        if (user != null)
                        {
                            db.Users.Remove(user);
                            db.SaveChanges();
                            MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUserList(txtSearch.Text.Trim());
                        }
                        else
                        {
                            MessageBox.Show("用户不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}
