using System;
using System.IO;
using System.Threading.Tasks;

namespace TailInstallationSystem
{
    public static class LogManager
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object LogLock = new object();

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug
        }

        static LogManager()
        {
            // 确保日志目录存在
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public static void LogInfo(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        public static void LogWarning(string message)
        {
            WriteLog(LogLevel.Warning, message);
        }

        public static void LogError(string message)
        {
            WriteLog(LogLevel.Error, message);
        }

        public static void LogDebug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        private static void WriteLog(LogLevel level, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{level}] {message}";

                lock (LogLock)
                {
                    // 写入到文件
                    var logFileName = $"TailInstallation_{DateTime.Now:yyyyMMdd}.log";
                    var logFilePath = Path.Combine(LogDirectory, logFileName);

                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }

                // 同时输出到控制台（用于调试）
                Console.WriteLine(logMessage);

                // 触发日志事件，用于UI显示
                OnLogWritten?.Invoke(level, timestamp, message);
            }
            catch (Exception ex)
            {
                // 日志写入失败时，至少输出到控制台
                Console.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        // 事件，用于UI实时显示日志
        public static event Action<LogLevel, string, string> OnLogWritten;

        public static async Task<string[]> GetRecentLogs(int lineCount = 100)
        {
            try
            {
                var logFileName = $"TailInstallation_{DateTime.Now:yyyyMMdd}.log";
                var logFilePath = Path.Combine(LogDirectory, logFileName);

                if (!File.Exists(logFilePath))
                {
                    return new string[0];
                }

                string[] lines = await Task.Run(() => File.ReadAllLines(logFilePath));
                var startIndex = Math.Max(0, lines.Length - lineCount);
                var result = new string[lines.Length - startIndex];

                Array.Copy(lines, startIndex, result, 0, result.Length);
                return result;
            }
            catch (Exception ex)
            {
                return new string[] { $"读取日志失败: {ex.Message}" };
            }
        }

        public static void ClearOldLogs(int keepDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var logFiles = Directory.GetFiles(LogDirectory, "TailInstallation_*.log");

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        LogInfo($"删除过期日志文件: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"清理过期日志失败: {ex.Message}");
            }
        }
    }
}