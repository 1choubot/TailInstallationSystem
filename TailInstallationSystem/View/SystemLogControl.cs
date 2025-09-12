using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TailInstallationSystem
{
    public partial class SystemLogControl : UserControl
    {
        private Timer autoRefreshTimer;
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
                Invoke(new Action<LogManager.LogLevel, string, string>(OnLogWritten), level, timestamp, message);
                return;
            }

            var logLine = $"[{timestamp}] [{GetLevelDisplayName(level)}] {message}";

            // 添加到缓冲区
            logBuffer.Add(logLine);

            // 限制缓冲区大小
            if (logBuffer.Count > 1000)
            {
                logBuffer.RemoveAt(0);
            }

            // 如果启用了自动刷新，立即显示新日志
            if (autoRefreshCheckBox.Checked)
            {
                AppendLogToDisplay(logLine, level);
            }
        }

        private void AppendLogToDisplay(string logLine, LogManager.LogLevel level)
        {
            // 设置日志颜色
            var color = GetLogLevelColor(level);

            logDisplayTextBox.SelectionStart = logDisplayTextBox.TextLength;
            logDisplayTextBox.SelectionColor = color;
            logDisplayTextBox.AppendText(logLine + Environment.NewLine);

            // 自动滚动到底部
            logDisplayTextBox.SelectionStart = logDisplayTextBox.TextLength;
            logDisplayTextBox.ScrollToCaret();

            // 限制显示的行数
            if (logDisplayTextBox.Lines.Length > 1000)
            {
                var lines = logDisplayTextBox.Lines;
                var newLines = new string[500];
                Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                logDisplayTextBox.Lines = newLines;
            }
        }

        private async void LoadRecentLogs()
        {
            try
            {
                UpdateStatus("正在加载日志...");
                SetButtonState(refreshButton, false, "加载中...");

                // 清空显示区域和缓冲区
                logDisplayTextBox.Clear();
                logBuffer.Clear();

                // 从日志文件加载最近500条
                var logs = await LogManager.GetRecentLogs(500);
                foreach (var logLine in logs)
                {
                    var level = ExtractLogLevel(logLine);
                    AppendLogToDisplay(logLine, level);
                    logBuffer.Add(logLine);
                }

                lastDisplayedLogCount = logBuffer.Count;
                UpdateStatus($"已加载 {logs.Length} 条日志记录");

                LogManager.LogInfo("日志显示已刷新");
            }
            catch (Exception ex)
            {
                UpdateStatus("加载日志失败");
                ShowMessage($"加载日志失败: {ex.Message}", MessageType.Error);
                LogManager.LogError($"加载日志失败: {ex.Message}");
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
                lastDisplayedLogCount = 0;
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

        // 移除重复的 Dispose 方法，改为 DisposeResources
        private void DisposeResources()
        {
            // 取消订阅事件
            LogManager.OnLogWritten -= OnLogWritten;

            // 停止并释放定时器
            if (autoRefreshTimer != null)
            {
                autoRefreshTimer.Stop();
                autoRefreshTimer.Dispose();
                autoRefreshTimer = null;
            }
        }

        // 重写 Dispose 方法
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeResources();

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
