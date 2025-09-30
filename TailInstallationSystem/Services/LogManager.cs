using System;
using System.IO;
using System.Threading.Tasks;

namespace TailInstallationSystem
{
    public static class LogManager
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object LogLock = new object();

        // 当前日志级别配置
        private static LogLevel _currentLogLevel = LogLevel.Info; // 默认级别

        public enum LogLevel
        {
            Debug = 0,    // Debug 级别最低
            Info = 1,     // Info 级别
            Warning = 2,  // Warning 级别
            Error = 3     // Error 级别最高
        }

        static LogManager()
        {
            // 确保日志目录存在
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        // 设置日志级别
        public static void SetLogLevel(string levelName)
        {
            try
            {
                if (Enum.TryParse<LogLevel>(levelName, true, out var level))
                {
                    _currentLogLevel = level;
                    // 移除这里的日志输出，让调用者决定是否记录
                }
                else
                {
                    WriteLog(LogLevel.Warning, $"无效的日志级别: {levelName}，保持当前级别: {_currentLogLevel}");
                }
            }
            catch (Exception ex)
            {
                WriteLog(LogLevel.Error, $"设置日志级别失败: {ex.Message}");
            }
        }

        // 设置日志级别（枚举版本）
        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
            WriteLog(LogLevel.Info, $"日志级别已设置为: {level}");
        }

        // 获取当前日志级别
        public static LogLevel GetCurrentLogLevel()
        {
            return _currentLogLevel;
        }

        // 检查是否应该记录某个级别的日志
        private static bool ShouldLog(LogLevel level)
        {
            // 只记录级别 >= 当前设置级别的日志
            // Debug=0, Info=1, Warning=2, Error=3
            // 当前级别为Info(1)时，只记录Info(1)、Warning(2)、Error(3)
            return (int)level >= (int)_currentLogLevel;
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

        // 支持日志级别过滤的写入方法
        private static void WriteLog(LogLevel level, string message)
        {
            try
            {
                // 检查是否应该记录此级别的日志
                if (!ShouldLog(level))
                {
                    return; // 跳过低级别日志
                }

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

        // 获取日志统计信息
        public static string GetLogStats()
        {
            try
            {
                var logFileName = $"TailInstallation_{DateTime.Now:yyyyMMdd}.log";
                var logFilePath = Path.Combine(LogDirectory, logFileName);

                if (!File.Exists(logFilePath))
                {
                    return "今日暂无日志";
                }

                var lines = File.ReadAllLines(logFilePath);
                var errorCount = 0;
                var warningCount = 0;
                var infoCount = 0;
                var debugCount = 0;

                foreach (var line in lines)
                {
                    if (line.Contains("[Error]")) errorCount++;
                    else if (line.Contains("[Warning]")) warningCount++;
                    else if (line.Contains("[Info]")) infoCount++;
                    else if (line.Contains("[Debug]")) debugCount++;
                }

                return $"今日日志统计 - 错误: {errorCount}, 警告: {warningCount}, 信息: {infoCount}, 调试: {debugCount}";
            }
            catch (Exception ex)
            {
                return $"获取日志统计失败: {ex.Message}";
            }
        }
    }
}
