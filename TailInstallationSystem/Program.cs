using System;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // 使用新的主窗体系统
                    Application.Run(new MainWindow());
                }
            }
        }
    }
}