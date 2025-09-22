using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TailInstallationSystem.View
{
    public partial class SystemLogControl : UserControl
    {
        private Timer autoRefreshTimer;
        private readonly Queue<string> logQueue = new Queue<string>();
        private const int MAX_BUFFER_SIZE = 1000;
        private List<string> logBuffer = new List<string>();
        private int lastDisplayedLogCount = 0;

        public SystemLogControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // 初始化自动刷新定时器
            autoRefreshTimer = new Timer();
            autoRefreshTimer.Interval = 5000; // 5秒刷新一次
            autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

            // 加载初始日志
            LoadRecentLogs();

            // 订阅日志事件
            LogManager.OnLogWritten += OnLogWritten;

            UpdateStatus("日志系统已初始化");
        }

        private void OnLogWritten(LogManager.LogLevel level, string timestamp, string message)
        {
            if (InvokeRequired)
            {
                // 使用BeginInvoke避免阻塞调用线程
                BeginInvoke(new Action<LogManager.LogLevel, string, string>(OnLogWritten), level, timestamp, message);
                return;
            }

            var logLine = $"[{timestamp}] [{GetLevelDisplayName(level)}] {message}";

            // Queue自动管理大小
            logQueue.Enqueue(logLine);
            if (logQueue.Count > MAX_BUFFER_SIZE)
            {
                logQueue.Dequeue();
            }

            // 同步更新List用于导出等功能
            logBuffer = logQueue.ToList();

            if (autoRefreshCheckBox.Checked)
            {
                AppendLogToDisplay(logLine, level);
            }
        }

        private void AppendLogToDisplay(string logLine, LogManager.LogLevel level)
        {
            // 暂停绘制，批量操作
            logDisplayTextBox.SuspendLayout();
            try
            {
                logDisplayTextBox.SelectionStart = logDisplayTextBox.TextLength;
                logDisplayTextBox.SelectionColor = GetLogLevelColor(level);
                logDisplayTextBox.AppendText(logLine + Environment.NewLine);

                // 优化行数限制逻辑
                var lineCount = logDisplayTextBox.Lines.Length;
                if (lineCount > 1000)
                {
                    // 使用更高效的文本截取
                    var text = logDisplayTextBox.Text;
                    var lines = text.Split('\n');
                    var keepLines = lines.Skip(lines.Length - 500).ToArray();
                    logDisplayTextBox.Text = string.Join("\n", keepLines);
                }

                // 只有在用户未手动滚动时才自动滚动
                if (!_userScrolled)
                {
                    logDisplayTextBox.SelectionStart = logDisplayTextBox.TextLength;
                    logDisplayTextBox.ScrollToCaret();
                }
            }
            finally
            {
                logDisplayTextBox.ResumeLayout();
            }
        }

        private bool _userScrolled = false;

        // 检测用户滚动状态
        private void logDisplayTextBox_Scroll(object sender, EventArgs e)
        {
            var rtb = sender as RichTextBox;
            var visibleLines = rtb.Height / rtb.Font.Height;
            var totalLines = rtb.Lines.Length;
            var firstVisibleLine = rtb.GetLineFromCharIndex(rtb.GetCharIndexFromPosition(new Point(0, 0)));
            
            _userScrolled = (firstVisibleLine + visibleLines) < totalLines - 2;
        }

        private async void LoadRecentLogs()
        {
            try
            {
                UpdateStatus("正在加载日志...");
                SetButtonState(refreshButton, false, "加载中...");

                // 在后台线程执行文件读取
                var logs = await Task.Run(async () => {
                    return await LogManager.GetRecentLogs(500).ConfigureAwait(false);
                });

                // 清空UI（UI线程操作）
                logDisplayTextBox.Clear();
                logQueue.Clear();
                logBuffer.Clear();

                // 批量添加到显示区域
                var batchText = new System.Text.StringBuilder();
                foreach (var logLine in logs)
                {
                    var level = ExtractLogLevel(logLine);
                    logQueue.Enqueue(logLine);
                    
                    // 批量构建文本，减少RichTextBox操作次数
                    batchText.AppendLine(logLine);
                }

                // 一次性设置文本内容
                logDisplayTextBox.Text = batchText.ToString();
                logBuffer = logQueue.ToList();
                lastDisplayedLogCount = logBuffer.Count;

                // 滚动到底部显示最新日志
                if (logDisplayTextBox.Text.Length > 0)
                {
                    logDisplayTextBox.SelectionStart = logDisplayTextBox.Text.Length;
                    logDisplayTextBox.ScrollToCaret();
                }

                // 重置用户滚动状态，因为这是程序加载，不是用户操作
                _userScrolled = false;

                UpdateStatus($"已加载 {logs.Length} 条日志记录");
            }
            catch (Exception ex)
            {
                UpdateStatus("加载日志失败");
                ShowMessage($"加载日志失败: {ex.Message}", MessageType.Error);
            }
            finally
            {
                SetButtonState(refreshButton, true, "刷新");
            }
        }

        // 替代 TakeLast 方法
        private List<T> GetLastItems<T>(List<T> list, int count)
        {
            if (list == null || list.Count == 0)
                return new List<T>();

            if (count >= list.Count)
                return new List<T>(list);

            var result = new List<T>();
            for (int i = list.Count - count; i < list.Count; i++)
            {
                result.Add(list[i]);
            }
            return result;
        }

        // 替代 TakeLast 方法，获取最后几个元素
        private List<T> GetLastItems<T>(List<T> list, int count, int skipFromEnd)
        {
            if (list == null || list.Count == 0)
                return new List<T>();

            var startIndex = Math.Max(0, list.Count - count - skipFromEnd);
            var endIndex = Math.Max(0, list.Count - skipFromEnd);
            var result = new List<T>();

            for (int i = startIndex; i < endIndex && i < list.Count; i++)
            {
                result.Add(list[i]);
            }
            return result;
        }

        private LogManager.LogLevel ExtractLogLevel(string logLine)
        {
            if (logLine.Contains("[ERROR]") || logLine.Contains("[错误]"))
                return LogManager.LogLevel.Error;
            else if (logLine.Contains("[WARNING]") || logLine.Contains("[警告]"))
                return LogManager.LogLevel.Warning;
            else if (logLine.Contains("[DEBUG]") || logLine.Contains("[调试]"))
                return LogManager.LogLevel.Debug;
            else
                return LogManager.LogLevel.Info;
        }

        private string GetLevelDisplayName(LogManager.LogLevel level)
        {
            switch (level)
            {
                case LogManager.LogLevel.Info: return "信息";
                case LogManager.LogLevel.Warning: return "警告";
                case LogManager.LogLevel.Error: return "错误";
                case LogManager.LogLevel.Debug: return "调试";
                default: return "未知";
            }
        }

        private Color GetLogLevelColor(LogManager.LogLevel level)
        {
            switch (level)
            {
                case LogManager.LogLevel.Info: return Color.Black;
                case LogManager.LogLevel.Warning: return Color.Orange;
                case LogManager.LogLevel.Error: return Color.Red;
                case LogManager.LogLevel.Debug: return Color.Gray;
                default: return Color.Black;
            }
        }

        private void SetButtonState(AntdUI.Button button, bool enabled, string text)
        {
            if (button != null)
            {
                button.Enabled = enabled;
                button.Loading = !enabled;
                button.Text = text;
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = $"状态: {message} - {DateTime.Now:HH:mm:ss}";
            }
        }

        private void ShowMessage(string message, MessageType type)
        {
            try
            {
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    switch (type)
                    {
                        case MessageType.Success:
                            AntdUI.Message.success(parentForm, message, autoClose: 3);
                            break;
                        case MessageType.Error:
                            AntdUI.Message.error(parentForm, message, autoClose: 3);
                            break;
                        case MessageType.Warning:
                            AntdUI.Message.warn(parentForm, message, autoClose: 3);
                            break;
                        case MessageType.Info:
                        default:
                            AntdUI.Message.info(parentForm, message, autoClose: 3);
                            break;
                    }
                }
                else
                {
                    MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #region 事件处理

        private void refreshButton_Click(object sender, EventArgs e)
        {
            LoadRecentLogs();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要清空日志显示吗？\n注意：这不会删除日志文件，只是清空显示区域。",
                "确认清空", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                logDisplayTextBox.Clear();
                logBuffer.Clear();
                logQueue.Clear(); // 同时清空队列
                lastDisplayedLogCount = 0;
                _userScrolled = false; // 重置滚动状态
                UpdateStatus("日志显示已清空");
                LogManager.LogInfo("日志显示已清空");
                ShowMessage("日志显示已清空", MessageType.Success);
            }
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|日志文件 (*.log)|*.log|所有文件 (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"SystemLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    UpdateStatus("正在导出日志...");
                    SetButtonState(exportButton, false, "导出中...");

                    // 导出所有日志缓冲区内容
                    var logContent = string.Join(Environment.NewLine, logBuffer.ToArray());
                    File.WriteAllText(saveDialog.FileName, logContent, System.Text.Encoding.UTF8);

                    UpdateStatus("日志导出成功");
                    ShowMessage("日志导出成功！", MessageType.Success);
                    LogManager.LogInfo($"日志已导出到: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    UpdateStatus("日志导出失败");
                    ShowMessage($"导出失败: {ex.Message}", MessageType.Error);
                    LogManager.LogError($"日志导出失败: {ex.Message}");
                }
                finally
                {
                    SetButtonState(exportButton, true, "📤 导出");
                }
            }
        }

        private void autoRefreshCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoRefreshCheckBox.Checked)
            {
                autoRefreshTimer.Start();
                UpdateStatus("自动刷新已启用");
                LogManager.LogInfo("日志自动刷新已启用");
            }
            else
            {
                autoRefreshTimer.Stop();
                UpdateStatus("自动刷新已禁用");
                LogManager.LogInfo("日志自动刷新已禁用");
            }
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            // 检查是否有新日志
            if (logBuffer.Count > lastDisplayedLogCount)
            {
                // 只显示新增的日志
                for (int i = lastDisplayedLogCount; i < logBuffer.Count; i++)
                {
                    var logLine = logBuffer[i];
                    var level = ExtractLogLevel(logLine);
                    AppendLogToDisplay(logLine, level);
                }
                lastDisplayedLogCount = logBuffer.Count;
            }
        }

        #endregion

        private void DisposeResources()
        {
            // 取消订阅事件
            LogManager.OnLogWritten -= OnLogWritten;

            // 停止并释放定时器
            if (autoRefreshTimer != null)
            {
                autoRefreshTimer.Stop();
                autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                autoRefreshTimer.Dispose();
                autoRefreshTimer = null;
            }

            // 清理缓冲区
            logQueue?.Clear();
            logBuffer?.Clear();
        }

        private volatile bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                
                // 确保事件取消订阅
                try
                {
                    LogManager.OnLogWritten -= OnLogWritten;
                }
                catch { /* 忽略取消订阅时的异常 */ }

                // 停止定时器
                if (autoRefreshTimer != null)
                {
                    autoRefreshTimer.Stop();
                    autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                    autoRefreshTimer.Dispose();
                    autoRefreshTimer = null;
                }

                // 清理缓冲区
                logQueue?.Clear();
                logBuffer?.Clear();

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        // 消息类型枚举
        public enum MessageType
        {
            Info,
            Success,
            Warning,
            Error
        }
    }
}
