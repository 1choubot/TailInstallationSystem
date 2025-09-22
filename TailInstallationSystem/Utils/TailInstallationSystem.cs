using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TailInstallationSystem.Utils
{
    public static class LocalFileManager
    {
        private static readonly string BaseDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ProductionData");

        /// <summary>
        /// 保存拼接后的生产数据到本地JSON文件
        /// </summary>
        /// <param name="barcode">扫描的条码（文件名）</param>
        /// <param name="process11Data">工序1的JSON数据</param>
        /// <param name="process12Data">工序2的JSON数据</param>
        /// <param name="process13Data">工序3的JSON数据</param>
        /// <param name="process14Data">工序4的JSON数据</param>
        public static async Task<bool> SaveProductionData(
            string barcode,
            string process11Data,
            string process12Data,
            string process13Data,
            string process14Data)
        {
            try
            {
                // 创建日期文件夹（格式：年-月-日）
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var dailyDirectory = Path.Combine(BaseDirectory, today);

                if (!Directory.Exists(dailyDirectory))
                {
                    Directory.CreateDirectory(dailyDirectory);
                    LogManager.LogInfo($"创建日期目录: {dailyDirectory}");
                }

                // 生成文件名（使用工序4扫描的条码）
                var fileName = $"{SanitizeFileName(barcode)}.json";
                var filePath = Path.Combine(dailyDirectory, fileName);

                // 构建拼接数据数组
                var processes = new object[4];

                // 解析各工序JSON并添加到数组
                processes[0] = TryParseJson(process11Data);  // 工序1：合装
                processes[1] = TryParseJson(process12Data);  // 工序2：气密性测试  
                processes[2] = TryParseJson(process13Data);  // 工序3：多功能测试
                processes[3] = TryParseJson(process14Data);  // 工序4：安装尾椎

                // 序列化为JSON字符串
                var jsonContent = JsonConvert.SerializeObject(processes, Formatting.Indented);

                // 异步保存到文件
                await Task.Run(() =>
                {
                    File.WriteAllText(filePath, jsonContent, Encoding.UTF8);
                });

                var fileSize = new FileInfo(filePath).Length;
                LogManager.LogInfo($"生产数据保存成功: {filePath} (大小: {fileSize} 字节)");

                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存生产数据失败: {barcode}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试解析JSON，失败则返回包含错误信息的对象
        /// </summary>
        private static object TryParseJson(string jsonData)
        {
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                return new { error = "数据为空" };
            }

            try
            {
                return JsonConvert.DeserializeObject(jsonData);
            }
            catch (Exception ex)
            {
                return new { error = "JSON解析失败", originalData = jsonData };
            }
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return $"Unknown_{DateTime.Now:HHmmss}";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            if (fileName.Length > 100)
            {
                fileName = fileName.Substring(0, 100);
            }

            return fileName;
        }

        /// <summary>
        /// 清理过期文件
        /// </summary>
        public static async Task CleanupOldFiles(int keepDays = 30)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(BaseDirectory))
                        return;

                    var cutoffDate = DateTime.Now.AddDays(-keepDays);
                    var directories = Directory.GetDirectories(BaseDirectory);

                    foreach (var dir in directories)
                    {
                        var dirName = Path.GetFileName(dir);

                        if (DateTime.TryParseExact(dirName, "yyyy-MM-dd", null,
                            System.Globalization.DateTimeStyles.None, out var dirDate))
                        {
                            if (dirDate < cutoffDate)
                            {
                                Directory.Delete(dir, true);
                                LogManager.LogInfo($"清理过期目录: {dirName}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"清理过期文件异常: {ex.Message}");
                }
            });
        }
    }
}
