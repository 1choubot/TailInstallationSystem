using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TailInstallationSystem.View;

namespace TailInstallationSystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // 订阅菜单项点击事件
            menu1.SelectChanged += Menu1_SelectChanged;
        }

        private void Menu1_SelectChanged(object sender, AntdUI.MenuSelectEventArgs e)
        {
            // 根据点击的菜单项名称加载对应界面
            switch (e.Value.Name)
            {
                case "AddUser":
                    LoadUserControl(new AddUser());
                    break;
                case "DeleteUser":
                     LoadUserControl(new DeleteUser());
                    break;
            }
        }

        private void LoadUserControl(UserControl userControl)
        {
            // 清除面板中现有的控件
            panelMain.Controls.Clear();

            // 设置基本属性
            userControl.Dock = DockStyle.None;
            userControl.Anchor = AnchorStyles.None;
            userControl.Margin = Padding.Empty;

            // 先添加到面板中
            panelMain.Controls.Add(userControl);

            // 强制更新布局，确保 panelMain 的尺寸是最新的
            panel1.PerformLayout();
            panelMain.PerformLayout();

            // 延迟执行居中，确保所有布局都已完成
            this.BeginInvoke(new Action(() =>
            {
                // 再次强制布局更新
                panelMain.PerformLayout();
                CenterUserControl(userControl);
            }));
        }

        private void CenterUserControl(UserControl userControl)
        {
            // 获取 panelMain 的实际客户区域
            Rectangle clientRect = panelMain.ClientRectangle;

            // 获取用户控件的实际尺寸
            Size actualSize = userControl.Size;
            if (actualSize.Width == 0 || actualSize.Height == 0)
            {
                actualSize = userControl.GetPreferredSize(Size.Empty);
                if (actualSize.Width == 0 || actualSize.Height == 0)
                {
                    actualSize = new Size(400, 300); // 默认尺寸
                }
                userControl.Size = actualSize;
            }

            // 计算居中位置 - 基于 ClientRectangle
            int x = clientRect.X + (clientRect.Width - actualSize.Width) / 2;
            int y = clientRect.Y + (clientRect.Height - actualSize.Height) / 2;

            userControl.Location = new Point(Math.Max(0, x), Math.Max(0, y));

            // 调试输出（可选）
            System.Diagnostics.Debug.WriteLine($"panelMain ClientRectangle: {clientRect}");
            System.Diagnostics.Debug.WriteLine($"UserControl Position: ({x}, {y})");
        }
    }
}
