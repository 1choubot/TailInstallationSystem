using System;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace TailInstallationSystem.Utils
{
    public class LicenseManager
    {
        private int activeType = 2;
        private bool activeIsOk = false;
        private DateTime activeTime;

        public bool IsLicenseValid => activeIsOk;
        public int ActiveType => activeType;

        public void CheckActive()
        {
            string strFileName = AppDomain.CurrentDomain.BaseDirectory + "Active.config";
            if (!new FileInfo(strFileName).Exists)
            {
                GenerateNewLicense();
                ValidateGeneratedLicense();
                return;
            }
            try
            {
                getActiveTime();
                DateTime currentTime = DateTime.Now;

                if (currentTime.Date > activeTime.Date) // 当前日期晚于授权到期日期
                {
                    activeType = 2; // 已过期
                    activeIsOk = false;
                    LogManager.LogWarning($"授权已过期 - 当前时间: {currentTime:yyyy-MM-dd}, 授权到期: {activeTime:yyyy-MM-dd}");
                }
                else
                {
                    int daysUntilExpiry = (activeTime.Date - currentTime.Date).Days;

                    if (daysUntilExpiry <= 30)
                    {
                        activeType = 1; // 30天内到期，提示
                        activeIsOk = true;
                        LogManager.LogWarning($"授权即将到期 - 剩余: {daysUntilExpiry} 天，到期时间: {activeTime:yyyy-MM-dd}");
                    }
                    else
                    {
                        activeType = 0; // 有效期内，通过
                        activeIsOk = true;
                        LogManager.LogInfo($"授权验证通过 - 剩余: {daysUntilExpiry} 天，到期时间: {activeTime:yyyy-MM-dd}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"授权验证失败: {ex.Message}");
                activeType = 2;
                activeIsOk = false;
            }
        }

        private void ValidateGeneratedLicense()
        {
            // 验证刚生成的授权码
            DateTime currentTime = DateTime.Now;

            if (currentTime > activeTime) // 当前时间超过授权时间
            {
                activeType = 2; // 已过期
                activeIsOk = false;
                LogManager.LogError($"生成的授权码无效 - 当前时间: {currentTime:yyyy-MM-dd}, 授权时间: {activeTime:yyyy-MM-dd}");
            }
            else
            {
                int daysUntilExpiry = (activeTime - currentTime).Days;

                if (daysUntilExpiry <= 30)
                {
                    activeType = 1; // 30天内到期，提示
                    activeIsOk = true;
                }
                else
                {
                    activeType = 0; // 有效期内，通过
                    activeIsOk = true;
                }
                LogManager.LogInfo($"授权码生成成功 - 有效期至: {activeTime:yyyy-MM-dd}, 剩余: {daysUntilExpiry}天");
            }
        }

        private void GenerateNewLicense()
        {
            try
            {
                // 生成一个新的授权码（有效期90天）
                DateTime futureDate = DateTime.Now.AddDays(90);
                string newCode = getNewCode(futureDate);
                // 创建配置文件
                CreateConfigFileIfNotExists();
                // 更新配置文件
                UpdateConfigFile(newCode);
                // 设置授权时间
                activeTime = futureDate;
                LogManager.LogInfo($"系统授权码已生成，有效期至: {futureDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"生成授权码失败: {ex.Message}");
                activeType = 2;
                activeIsOk = false;
            }
        }

        private void CreateConfigFileIfNotExists()
        {
            string strFileName = AppDomain.CurrentDomain.BaseDirectory + "Active.config";
            if (!File.Exists(strFileName))
            {
                // 创建基本的配置文件结构
                string configContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <configuration>
                    <appSettings>
                        <add key=""CODE"" value="""" />
                    </appSettings>
                </configuration>";
                File.WriteAllText(strFileName, configContent);
                LogManager.LogInfo("授权配置文件已创建");
            }
        }

        private void UpdateConfigFile(string newCode)
        {
            try
            {
                string strFileName = AppDomain.CurrentDomain.BaseDirectory + "Active.config";
                XmlDocument doc = new XmlDocument();
                doc.Load(strFileName);
                XmlNodeList nodes = doc.GetElementsByTagName("add");
                bool updated = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlAttribute keyAtt = nodes[i].Attributes["key"];
                    if (keyAtt != null && keyAtt.Value == "CODE")
                    {
                        XmlAttribute valueAtt = nodes[i].Attributes["value"];
                        if (valueAtt != null)
                        {
                            valueAtt.Value = newCode;
                            updated = true;
                            break;
                        }
                    }
                }
                if (updated)
                {
                    doc.Save(strFileName);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新授权配置失败: {ex.Message}");
                throw;
            }
        }

        private void getActiveTime()
        {
            XmlDocument doc = new XmlDocument();
            string strFileName = AppDomain.CurrentDomain.BaseDirectory + "Active.config";
            doc.Load(strFileName);
            XmlNodeList nodes = doc.GetElementsByTagName("add");
            bool foundValidLicense = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlAttribute keyAtt = nodes[i].Attributes["key"];
                XmlAttribute valueAtt = nodes[i].Attributes["value"];
                if (keyAtt != null && keyAtt.Value == "CODE" && valueAtt != null)
                {
                    try
                    {
                        string code = valueAtt.Value;
                        // 验证授权码长度
                        if (string.IsNullOrEmpty(code) || code.Length < 30)
                        {
                            throw new Exception("授权码格式无效");
                        }
                        // 解析年份长度 (第8位，索引8)
                        int yearLen = int.Parse(code.Substring(8, 1));
                        // 解析月份长度 (第17位，索引17) 
                        int monthLen = int.Parse(code.Substring(17, 1));
                        // 解析日期长度 (第26位，索引26)
                        int dayLen = int.Parse(code.Substring(26, 1));
                        // 从第27位开始提取日期数据
                        string yearHex = code.Substring(27, yearLen);
                        string monthHex = code.Substring(27 + yearLen, monthLen);
                        string dayHex = code.Substring(27 + yearLen + monthLen, dayLen);
                        // 转换为十进制
                        int year = Convert.ToInt32(yearHex, 16);
                        int month = Convert.ToInt32(monthHex, 16);
                        int day = Convert.ToInt32(dayHex, 16);
                        activeTime = new DateTime(year, month, day);
                        foundValidLicense = true;
                        #if DEBUG
                        LogManager.LogDebug($"授权码解析成功: {activeTime:yyyy-MM-dd}");
                        #endif
                        break;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"授权码解析失败: {ex.Message}");
                    }
                }
            }
            if (!foundValidLicense)
            {
                throw new Exception("未找到有效的授权码");
            }
        }

        public void SaveConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                string strFileName = AppDomain.CurrentDomain.BaseDirectory + "Active.config";
                doc.Load(strFileName);

                XmlNodeList nodes = doc.GetElementsByTagName("add");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlAttribute keyAtt = nodes[i].Attributes["key"];
                    if (keyAtt != null && keyAtt.Value == "CODE")
                    {
                        XmlAttribute valueAtt = nodes[i].Attributes["value"];
                        if (valueAtt != null)
                        {
                            // 延期90天
                            valueAtt.Value = getNewCode(DateTime.Now.AddDays(90));
                        }
                        break;
                    }
                }
                doc.Save(strFileName);
                LogManager.LogInfo("授权已更新");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新授权失败: {ex.Message}");
            }
        }

        private string getNewCode(DateTime targetDate)
        {
            string guidStr = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            // 确保有足够的字符
            if (guidStr.Length < 32)
            {
                guidStr = (guidStr + "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456").Substring(0, 32);
            }
            string yearHex = targetDate.Year.ToString("X");   // 年份转16进制
            string monthHex = targetDate.Month.ToString("X"); // 月份转16进制  
            string dayHex = targetDate.Day.ToString("X");     // 日期转16进制
                                                              // 生成授权码：8位GUID + 年长度 + 8位GUID + 月长度 + 8位GUID + 日长度 + 年月日 + 8位GUID
            string code = guidStr.Substring(0, 8) +
                          yearHex.Length.ToString() +
                          guidStr.Substring(8, 8) +
                          monthHex.Length.ToString() +
                          guidStr.Substring(16, 8) +
                          dayHex.Length.ToString() +
                          yearHex + monthHex + dayHex +
                          guidStr.Substring(24, 8);
            #if DEBUG
            LogManager.LogDebug($"授权码生成: {targetDate:yyyy-MM-dd}");
            #endif

            return code;
        }

        public bool ShowActive()
        {
            DateTime now = DateTime.Now;
            int actualDays = Math.Max(0, (activeTime.Date - now.Date).Days);

            switch (activeType)
            {
                case 2:
                    string errorMessage = $"您的系统已经超过有效期，请联系厂家维护后使用\n\n" +
                                $"当前时间: {now:yyyy-MM-dd}\n" +
                                $"授权到期: {activeTime:yyyy-MM-dd}";

                    LogManager.LogError($"系统授权已过期");
                    MessageBox.Show(errorMessage, "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    activeIsOk = false;
                    break;

                case 1:
                    MessageBox.Show(
                        $"您的系统即将超过有效期，请尽快联系厂家维护\n\n" +
                        $"当前时间: {now:yyyy-MM-dd}\n" +
                        $"授权到期: {activeTime:yyyy-MM-dd}\n" +
                        $"剩余天数: {actualDays} 天",
                        "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    activeIsOk = true;
                    break;

                case 0:
                    activeIsOk = true;
                    break;
            }
            return activeIsOk;
        }

        public string GetLicenseStatus()
        {
            try
            {
                CheckActive();
                if (activeTime == default(DateTime))
                {
                    return "授权信息无效";
                }
                int daysRemaining = Math.Max(0, (activeTime - DateTime.Now).Days);
                return $"授权有效期至: {activeTime:yyyy-MM-dd} (剩余 {daysRemaining} 天)";
            }
            catch
            {
                return "获取授权信息失败";
            }
        }
    }
}
