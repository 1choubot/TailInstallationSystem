using OfficeOpenXml;
using System;
using System.Threading;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                LogManager.LogInfo("EPPlus 7.3.2 授权设置成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"EPPlus授权设置失败：{ex.Message}",
                    "初始化警告",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            // 设置全局异常处理模式
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // 绑定全局异常处理事件
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 初始化日志系统
            InitializeLogging();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                LogManager.LogInfo("应用程序启动开始");

                // 显示登录窗口
                using (var loginForm = new LoginForm())
                {
                    var loginResult = loginForm.ShowDialog();

                    if (loginResult == DialogResult.OK)
                    {
                        LogManager.LogInfo("用户登录成功，启动主窗口");

                        // 创建并运行主窗口
                        var mainWindow = new MainWindow();

                        // 绑定主窗口关闭事件
                        mainWindow.FormClosed += MainWindow_FormClosed;

                        Application.Run(mainWindow);
                    }
                    else
                    {
                        LogManager.LogInfo("用户取消登录，程序退出");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleCriticalException("程序主入口", ex);
            }
            finally
            {
                try
                {
                    // 程序退出时的清理工作
                    PerformCleanup();
                    LogManager.LogInfo("应用程序正常退出");
                }
                catch (Exception ex)
                {
                    // 清理过程中的异常不应该阻止程序退出
                    try
                    {
                        LogManager.LogError($"程序清理过程异常: {ex.Message}");
                    }
                    catch
                    {
                        // 如果连日志都无法写入，就静默退出
                    }
                }
            }
        }

        /// <summary>
        /// 处理UI线程异常
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                LogManager.LogError($"应用程序线程异常: {e.Exception.Message}");
                LogManager.LogError($"异常类型: {e.Exception.GetType().Name}");
                LogManager.LogError($"异常堆栈: {e.Exception.StackTrace}");

                // 记录内部异常
                if (e.Exception.InnerException != null)
                {
                    LogManager.LogError($"内部异常: {e.Exception.InnerException.Message}");
                }

                // 检查是否为严重异常
                if (IsCriticalException(e.Exception))
                {
                    HandleCriticalException("UI线程", e.Exception);
                    return;
                }

                // 非严重异常，显示用户友好的错误信息
                var result = ShowErrorDialog(
                    "应用程序异常",
                    $"程序遇到一个错误，但可以尝试继续运行：\n\n{GetUserFriendlyErrorMessage(e.Exception)}\n\n是否继续运行程序？",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    LogManager.LogInfo("用户选择退出程序");
                    Application.Exit();
                }
                else
                {
                    LogManager.LogInfo("用户选择继续运行程序");
                }
            }
            catch (Exception logEx)
            {
                // 如果异常处理本身出现异常，尝试最基本的错误显示
                try
                {
                    MessageBox.Show(
                        $"程序遇到严重错误，无法正常处理异常：\n原始异常: {e.Exception.Message}\n处理异常: {logEx.Message}",
                        "严重系统错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // 最后的保护措施 - 强制退出
                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// 处理非UI线程和域异常
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;

                LogManager.LogError($"未处理的域异常: {exception?.Message ?? "未知异常"}");
                LogManager.LogError($"异常类型: {exception?.GetType().Name ?? "未知类型"}");
                LogManager.LogError($"程序即将终止: {e.IsTerminating}");

                if (exception != null)
                {
                    LogManager.LogError($"异常堆栈: {exception.StackTrace}");

                    if (exception.InnerException != null)
                    {
                        LogManager.LogError($"内部异常: {exception.InnerException.Message}");
                    }
                }

                if (e.IsTerminating)
                {
                    // 程序即将终止，尝试保存关键数据
                    try
                    {
                        PerformEmergencyCleanup();

                        ShowErrorDialog(
                            "严重系统错误",
                            $"程序遇到严重错误即将退出：\n\n{GetUserFriendlyErrorMessage(exception)}\n\n请查看日志文件获取详细信息。\n程序将在确认后退出。",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch
                    {
                        // 紧急清理失败，直接退出
                    }
                }
            }
            catch (Exception logEx)
            {
                // 异常处理出现异常，尝试写入系统日志或文件
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Application.StartupPath, "CriticalError.log"),
                        $"[{DateTime.Now}] Critical Exception Handling Failed: {logEx.Message}\n" +
                        $"Original Exception: {e.ExceptionObject}\n\n");
                }
                catch
                {
                    // 最后的保护措施
                }
            }
        }

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        private static void InitializeLogging()
        {
            try
            {
                // 确保日志目录存在
                var logDir = System.IO.Path.Combine(Application.StartupPath, "Logs");
                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                LogManager.LogInfo("日志系统初始化完成");
            }
            catch (Exception ex)
            {
                // 日志系统初始化失败，使用MessageBox通知
                MessageBox.Show(
                    $"日志系统初始化失败: {ex.Message}\n程序将继续运行，但可能无法记录日志。",
                    "初始化警告",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 主窗口关闭事件处理
        /// </summary>
        private static void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                LogManager.LogInfo("主窗口已关闭");
            }
            catch
            {
                // 忽略日志记录异常
            }
        }

        /// <summary>
        /// 判断是否为严重异常
        /// </summary>
        private static bool IsCriticalException(Exception ex)
        {
            return ex is OutOfMemoryException ||
                   ex is StackOverflowException ||
                   ex is AccessViolationException ||
                   ex is AppDomainUnloadedException ||
                   ex is BadImageFormatException ||
                   ex is CannotUnloadAppDomainException ||
                   ex is ExecutionEngineException ||
                   ex is InvalidProgramException ||
                   ex is ThreadAbortException;
        }

        /// <summary>
        /// 处理严重异常
        /// </summary>
        private static void HandleCriticalException(string source, Exception ex)
        {
            try
            {
                LogManager.LogError($"严重异常发生在 {source}: {ex?.Message}");
                LogManager.LogError($"严重异常类型: {ex?.GetType().Name}");
                LogManager.LogError($"严重异常堆栈: {ex?.StackTrace}");

                // 紧急清理
                PerformEmergencyCleanup();

                ShowErrorDialog(
                    "严重系统错误",
                    $"程序遇到严重的系统级错误，必须立即退出：\n\n错误类型: {ex?.GetType().Name}\n错误信息: {ex?.Message}\n\n程序将立即关闭以防止数据损坏。",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
            catch
            {
                // 严重异常处理失败，直接退出
            }
            finally
            {
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// 获取用户友好的错误信息
        /// </summary>
        private static string GetUserFriendlyErrorMessage(Exception ex)
        {
            if (ex == null) return "未知错误";

            switch (ex)
            {
                case OutOfMemoryException _:
                    return "系统内存不足，请关闭其他程序后重试。";

                case UnauthorizedAccessException _:
                    return "权限不足，请以管理员身份运行程序。";

                case System.IO.FileNotFoundException _:
                case System.IO.DirectoryNotFoundException _:
                    return "找不到必要的文件或目录，请检查程序安装是否完整。";

                case System.Net.NetworkInformation.NetworkInformationException _:
                case System.Net.Sockets.SocketException _:
                    return "网络连接异常，请检查网络设置。";

                case TimeoutException _:
                    return "操作超时，请检查网络连接或稍后重试。";

                case InvalidOperationException _:
                    return "程序状态异常，建议重启程序。";

                default:
                    // 对于其他异常，提供通用消息但包含具体错误信息
                    return $"程序运行异常: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示错误对话框（线程安全）
        /// </summary>
        private static DialogResult ShowErrorDialog(string title, string message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                // 确保在UI线程上显示对话框
                if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
                {
                    return (DialogResult)Application.OpenForms[0].Invoke(new Func<DialogResult>(() =>
                        MessageBox.Show(message, title, buttons, icon)));
                }
                else
                {
                    return MessageBox.Show(message, title, buttons, icon);
                }
            }
            catch
            {
                // 如果对话框显示失败，返回默认值
                return DialogResult.OK;
            }
        }

        /// <summary>
        /// 执行正常清理
        /// </summary>
        private static void PerformCleanup()
        {
            try
            {
                LogManager.LogInfo("开始执行程序清理...");

                // 清理临时文件
                CleanupTempFiles();

                // 保存用户设置
                try
                {
                    Properties.Settings.Default.Save();
                    LogManager.LogInfo("用户设置已保存");
                }
                catch (Exception ex)
                {
                    LogManager.LogWarning($"保存用户设置失败: {ex.Message}");
                }

                LogManager.LogInfo("程序清理完成");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"程序清理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行紧急清理（用于严重异常情况）
        /// </summary>
        private static void PerformEmergencyCleanup()
        {
            try
            {
                LogManager.LogInfo("开始执行紧急清理...");

                // 尝试保存关键数据
                try
                {
                    Properties.Settings.Default.Save();
                }
                catch { }

                // 清理关键资源
                CleanupCriticalResources();

                LogManager.LogInfo("紧急清理完成");
            }
            catch (Exception ex)
            {
                try
                {
                    LogManager.LogError($"紧急清理异常: {ex.Message}");
                }
                catch { }
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private static void CleanupTempFiles()
        {
            try
            {
                var tempDir = System.IO.Path.Combine(Application.StartupPath, "Temp");
                if (System.IO.Directory.Exists(tempDir))
                {
                    var files = System.IO.Directory.GetFiles(tempDir);
                    foreach (var file in files)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch
                        {
                            // 忽略单个文件删除失败
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"清理临时文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理关键资源
        /// </summary>
        private static void CleanupCriticalResources()
        {
            try
            {
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 其他关键资源清理可以在这里添加

            }
            catch (Exception ex)
            {
                try
                {
                    LogManager.LogWarning($"清理关键资源失败: {ex.Message}");
                }
                catch { }
            }
        }
    }
}
