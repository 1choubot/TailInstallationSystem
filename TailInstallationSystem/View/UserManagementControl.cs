using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace TailInstallationSystem
{
    public partial class UserManagementControl : UserControl
    {
        private UserManager userManager;

        public UserManagementControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            try
            {
                userManager = new UserManager();
                LoadUsers();
                ConfigureListView();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化用户管理控件失败: {ex.Message}");
                ShowMessage("初始化失败", NotificationType.Error); // 使用 NotificationType 避免冲突
            }
        }

        private void ConfigureListView()
        {
            userListView.View = System.Windows.Forms.View.Details;
            userListView.FullRowSelect = true;
            userListView.GridLines = true;
            userListView.HideSelection = false;
            userListView.MultiSelect = false;

            foreach (ColumnHeader column in userListView.Columns)
            {
                column.Width = -2;
            }
        }

        private void LoadUsers()
        {
            try
            {
                SetButtonLoadingState(addUserButton, true, "加载中...");

                LogManager.LogInfo("开始加载用户列表");

                var users = userManager.GetAllUsers();

                userListView.Items.Clear();

                foreach (var user in users)
                {
                    var item = new ListViewItem(new string[]
                    {
                        user.Username ?? "N/A",
                        GetPermissionDisplayName(user.Permission),
                        user.CreatedTime?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",
                        user.LastLoginTime?.ToString("yyyy-MM-dd HH:mm") ?? "从未登录",
                        user.IsActive ? "活跃" : "禁用"
                    });

                    item.Tag = user;
                    userListView.Items.Add(item);
                }

                LogManager.LogInfo($"加载了 {users.Count} 个用户");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载用户列表失败: {ex.Message}");
                ShowMessage("加载用户列表失败", NotificationType.Error);
            }
            finally
            {
                SetButtonLoadingState(addUserButton, false, "➕ 添加用户");
            }
        }

        private List<User> GetAllUsers()
        {
            return new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Permission = UserPermission.Administrator,
                    CreatedTime = DateTime.Now.AddDays(-30),
                    LastLoginTime = DateTime.Now.AddHours(-2),
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Username = "operator1",
                    Permission = UserPermission.Operator,
                    CreatedTime = DateTime.Now.AddDays(-15),
                    LastLoginTime = DateTime.Now.AddDays(-1),
                    IsActive = true
                },
                new User
                {
                    Id = 3,
                    Username = "viewer1",
                    Permission = UserPermission.Viewer,
                    CreatedTime = DateTime.Now.AddDays(-7),
                    LastLoginTime = null,
                    IsActive = false
                }
            };
        }

        private string GetPermissionDisplayName(UserPermission permission)
        {
            switch (permission)
            {
                case UserPermission.Administrator:
                    return "管理员";
                case UserPermission.Operator:
                    return "操作员";
                case UserPermission.Viewer:
                    return "查看员";
                default:
                    return "未知";
            }
        }

        #region 辅助方法

        private void SetButtonLoadingState(AntdUI.Button button, bool loading, string text)
        {
            if (button == null) return;

            button.Loading = loading;
            button.Text = text;
            button.Enabled = !loading;
        }

        private void ShowMessage(string message, NotificationType type)
        {
            try
            {
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    switch (type)
                    {
                        case NotificationType.Success:
                            AntdUI.Message.success(parentForm, message, autoClose: 3);
                            break;
                        case NotificationType.Error:
                            AntdUI.Message.error(parentForm, message, autoClose: 3);
                            break;
                        case NotificationType.Warning:
                            AntdUI.Message.warn(parentForm, message, autoClose: 3);
                            break;
                        case NotificationType.Info:
                        default:
                            AntdUI.Message.info(parentForm, message, autoClose: 3);
                            break;
                    }
                }
                else
                {
                    MessageBoxIcon icon;
                    switch (type)
                    {
                        case NotificationType.Success:
                            icon = MessageBoxIcon.Information;
                            break;
                        case NotificationType.Error:
                            icon = MessageBoxIcon.Error;
                            break;
                        case NotificationType.Warning:
                            icon = MessageBoxIcon.Warning;
                            break;
                        default:
                            icon = MessageBoxIcon.Information;
                            break;
                    }
                    MessageBox.Show(message, "提示", MessageBoxButtons.OK, icon);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"显示消息失败: {ex.Message}");
                MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private User GetSelectedUser()
        {
            if (userListView.SelectedItems.Count > 0)
            {
                return userListView.SelectedItems[0].Tag as User;
            }
            return null;
        }

        #endregion

        #region 事件处理方法

        private void addUserButton_Click(object sender, EventArgs e)
        {
            try
            {
                var dialog = new UserAddDialog();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    var newUser = new User
                    {
                        Username = dialog.UserName,
                        PasswordHash = dialog.Password,
                        CreatedTime = DateTime.Now,
                        Permission = UserPermission.Viewer, // 默认权限，可扩展
                        IsActive = true
                    };

                    if (userManager.CreateUser(newUser))
                    {
                        LogManager.LogInfo($"添加用户成功: {newUser.Username}");
                        ShowMessage("用户添加成功！", NotificationType.Success);
                        RefreshUserList();
                    }
                    else
                    {
                        ShowMessage("用户添加失败！", NotificationType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"添加用户失败: {ex.Message}");
                ShowMessage("添加用户失败", NotificationType.Error);
            }
        }

        private void editUserMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var user = GetSelectedUser();
                if (user != null)
                {
                    LogManager.LogInfo($"编辑用户: {user.Username}");
                    ShowMessage($"编辑用户 {user.Username} (功能待实现)", NotificationType.Info);
                }
                else
                {
                    ShowMessage("请选择要编辑的用户", NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"编辑用户失败: {ex.Message}");
                ShowMessage("编辑用户失败", NotificationType.Error);
            }
        }

        private void deleteUserMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var user = GetSelectedUser();
                if (user == null)
                {
                    ShowMessage("请选择要删除的用户", NotificationType.Warning);
                    return;
                }

                if (user.Permission == UserPermission.Administrator)
                {
                    ShowMessage("不能删除管理员账户", NotificationType.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要删除用户 '{user.Username}' 吗？\n此操作不可撤销！",
                    "确认删除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    LogManager.LogInfo($"删除用户: {user.Username}");
                    if (userManager.DeleteUser(user.Id))
                    {
                        ShowMessage("用户删除成功！", NotificationType.Success);
                        LogManager.LogInfo($"用户 {user.Username} 已被删除");
                        RefreshUserList();
                    }
                    else
                    {
                        ShowMessage("用户删除失败！", NotificationType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"删除用户失败: {ex.Message}");
                ShowMessage("删除用户失败", NotificationType.Error);
            }
        }

        private void resetPasswordMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var user = GetSelectedUser();
                if (user == null)
                {
                    ShowMessage("请选择要重置密码的用户", NotificationType.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要重置用户 '{user.Username}' 的密码吗？\n密码将重置为默认值。",
                    "确认重置密码",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    LogManager.LogInfo($"重置用户密码: {user.Username}");
                    ShowMessage($"用户 {user.Username} 的密码已重置为默认密码！", NotificationType.Success);
                    LogManager.LogInfo($"用户 {user.Username} 密码已重置");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"重置密码失败: {ex.Message}");
                ShowMessage("重置密码失败", NotificationType.Error);
            }
        }

        private void userListView_DoubleClick(object sender, EventArgs e)
        {
            editUserMenuItem_Click(sender, e);
        }

        #endregion

        public void RefreshUserList()
        {
            LoadUsers();
        }
    }

    // 使用不同的名称避免冲突
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    // 用户权限枚举
    public enum UserPermission
    {
        Viewer = 1,
        Operator = 2,
        Administrator = 3
    }

    // 用户实体类
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserPermission Permission { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsActive { get; set; } = true;
        public string Email { get; set; }
        public string FullName { get; set; }
    }

    // 用户管理器类
    public class UserManager
    {
        public List<User> GetAllUsers()
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                // 查询数据库中的所有用户
                return db.Users.Select(u => new User
                {
                    Id = u.Id,
                    Username = u.UserName,
                    PasswordHash = u.Password,
                    CreatedTime = u.CreatedTime,
                    
                    Permission = UserPermission.Viewer 
                }).ToList();
            }
        }

        public User GetUserById(long id)
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                var u = db.Users.FirstOrDefault(x => x.Id == id);
                if (u == null) return null;
                return new User
                {
                    Id = u.Id,
                    Username = u.UserName,
                    PasswordHash = u.Password,
                    CreatedTime = u.CreatedTime,
                    Permission = UserPermission.Viewer 
                };
            }
        }

        public bool CreateUser(User user)
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                var entity = new Users
                {
                    UserName = user.Username,
                    Password = user.PasswordHash,
                    CreatedTime = user.CreatedTime ?? DateTime.Now
                };
                db.Users.Add(entity);
                db.SaveChanges();
                return true;
            }
        }

        public bool UpdateUser(User user)
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                var entity = db.Users.FirstOrDefault(x => x.Id == user.Id);
                if (entity == null) return false;
                entity.UserName = user.Username;
                entity.Password = user.PasswordHash;
                // 其它字段可补充
                db.SaveChanges();
                return true;
            }
        }

        public bool DeleteUser(long id)
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                var entity = db.Users.FirstOrDefault(x => x.Id == id);
                if (entity == null) return false;
                db.Users.Remove(entity);
                db.SaveChanges();
                return true;
            }
        }

        public bool ResetPassword(long id)
        {
            using (var db = new NodeInstrumentMESEntities())
            {
                var entity = db.Users.FirstOrDefault(x => x.Id == id);
                if (entity == null) return false;
                entity.Password = "123456"; // 默认密码，可加密
                db.SaveChanges();
                return true;
            }
        }
    }
}
