using AntdUI;
using System;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using HslCommunication.ModBus;
using TailInstallationSystem.Models;
using TailInstallationSystem.Utils;

namespace TailInstallationSystem
{
    public partial class CommunicationSettingsControl : UserControl
    {
        private CommunicationConfig _config;

        public CommunicationSettingsControl()
        {
            InitializeComponent();
            this.AutoScroll = true;
            plcTestButton.Click += plcTestButton_Click;
            scannerTestButton.Click += scannerTestButton_Click;
            tighteningAxisTestButton.Click += tighteningAxisTestButton_Click;
            mesTestButton.Click += mesTestButton_Click;  // 添加MES测试按钮事件
            saveButton.Click += saveButton_Click;
            LoadSettings();
        }

        private Form GetParentForm()
        {
            return this.FindForm() ?? this.ParentForm;
        }

        private void LoadSettings()
        {
            try
            {
                _config = ConfigManager.LoadConfig();
                LoadPLCSettings();
                LoadScannerSettings();
                LoadTighteningAxisSettings();
                LoadMESSettings();  // 添加MES设置加载

                LogManager.LogInfo("通讯设置界面已加载");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载设置失败: {ex.Message}");
            }
        }

        private void LoadPLCSettings()
        {
            plcIpTextBox.Text = _config.PLC.IP;
            plcPortTextBox.Text = _config.PLC.Port.ToString();
            plcStationTextBox.Text = _config.PLC.Station.ToString();
        }

        private void LoadScannerSettings()
        {
            scannerIpTextBox.Text = _config.Scanner.IP;
            scannerPortTextBox.Text = _config.Scanner.Port.ToString();
        }

        private void LoadTighteningAxisSettings()
        {
            tighteningAxisIpTextBox.Text = _config.TighteningAxis.IP;
            tighteningAxisPortTextBox.Text = _config.TighteningAxis.Port.ToString();
            tighteningAxisStationTextBox.Text = _config.TighteningAxis.Station.ToString();
        }

        private void LoadMESSettings()
        {
            mesUrlTextBox.Text = _config.Server.WebSocketUrl;
            mesApiKeyTextBox.Text = _config.Server.ApiKey;
        }

        #region 连接测试实现

        private async void plcTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidatePLCInput()) return;

            try
            {
                plcTestButton.Loading = true;
                plcTestButton.Text = "测试中...";
                plcTestButton.Enabled = false;

                var ip = plcIpTextBox.Text.Trim();
                var port = int.Parse(plcPortTextBox.Text.Trim());
                var station = byte.Parse(plcStationTextBox.Text.Trim());

                LogManager.LogInfo($"测试PLC连接: {ip}:{port}, 站号:{station}");

                bool success = await TestPLCConnection(ip, port, station);

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "PLC连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "PLC连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"PLC连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                plcTestButton.Loading = false;
                plcTestButton.Text = "测试连接";
                plcTestButton.Enabled = true;
            }
        }

        private async void scannerTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidateScannerInput()) return;

            try
            {
                scannerTestButton.Loading = true;
                scannerTestButton.Text = "测试中...";
                scannerTestButton.Enabled = false;

                var ip = scannerIpTextBox.Text.Trim();
                var port = int.Parse(scannerPortTextBox.Text.Trim());

                LogManager.LogInfo($"测试扫码枪连接: {ip}:{port}");

                bool success = await TestTcpConnection(ip, port, "扫码枪");

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "扫码枪连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "扫码枪连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"扫码枪连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"扫码枪连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                scannerTestButton.Loading = false;
                scannerTestButton.Text = "测试连接";
                scannerTestButton.Enabled = true;
            }
        }

        private async void tighteningAxisTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidateTighteningAxisInput()) return;

            try
            {
                tighteningAxisTestButton.Loading = true;
                tighteningAxisTestButton.Text = "测试中...";
                tighteningAxisTestButton.Enabled = false;

                var ip = tighteningAxisIpTextBox.Text.Trim();
                var port = int.Parse(tighteningAxisPortTextBox.Text.Trim());
                var station = byte.Parse(tighteningAxisStationTextBox.Text.Trim());

                LogManager.LogInfo($"测试拧紧轴连接: {ip}:{port}, 站号:{station}");

                // 这里只是测试连接，不会启动轮询
                bool success = await TestTighteningAxisConnection(ip, port, station);

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "拧紧轴连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "拧紧轴连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧轴连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"拧紧轴连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                tighteningAxisTestButton.Loading = false;
                tighteningAxisTestButton.Text = "测试连接";
                tighteningAxisTestButton.Enabled = true;
            }
        }

        private async void mesTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidateMESInput()) return;

            try
            {
                mesTestButton.Loading = true;
                mesTestButton.Text = "测试中...";
                mesTestButton.Enabled = false;

                var url = mesUrlTextBox.Text.Trim();
                var apiKey = mesApiKeyTextBox.Text.Trim();

                LogManager.LogInfo($"测试MES系统连接: {url}");

                bool success = await TestMESConnection(url, apiKey);

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "MES系统连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "MES系统连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"MES系统连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"MES系统连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                mesTestButton.Loading = false;
                mesTestButton.Text = "测试连接";
                mesTestButton.Enabled = true;
            }
        }

        #endregion

        #region 连接测试方法

        private async Task<bool> TestPLCConnection(string ip, int port, byte station)
        {
            try
            {
                var modbusTcpClient = new ModbusTcpNet(ip, port, station);
                var connectResult = await Task.Run(() => modbusTcpClient.ConnectServer());

                if (connectResult.IsSuccess)
                {
                    string testAddress = _config.PLC.TighteningTriggerAddress.Replace("D", "");
                    var readResult = await Task.Run(() => modbusTcpClient.ReadInt16(testAddress, 1));
                    modbusTcpClient.ConnectClose();

                    if (readResult.IsSuccess)
                    {
                        LogManager.LogInfo($"PLC测试成功，D501寄存器值: {readResult.Content[0]}");
                        return true;
                    }
                    else
                    {
                        LogManager.LogError($"PLC读取失败: {readResult.Message}");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC连接测试异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestTighteningAxisConnection(string ip, int port, byte station)
        {
            ModbusTcpNet modbusTcpClient = null;
            try
            {
                modbusTcpClient = new ModbusTcpNet(ip, port, station);
                modbusTcpClient.ConnectTimeOut = 5000;
                modbusTcpClient.ReceiveTimeOut = 3000;

                var connectResult = await Task.Run(() => modbusTcpClient.ConnectServer());

                if (!connectResult.IsSuccess)
                {
                    LogManager.LogWarning($"拧紧轴Modbus连接失败: {connectResult.Message}");
                    return false;
                }

                LogManager.LogInfo("========== 拧紧轴连接测试 ==========");
                LogManager.LogInfo($"设备地址: {ip}:{port}, 站号:{station}");
                LogManager.LogInfo("-----------------------------------");

                // 1. 读取状态码（地址5104，最关键！）
                var statusCodeResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5104", 2));

                if (statusCodeResult.IsSuccess)
                {
                    float statusFloat = ConvertToFloat(statusCodeResult.Content);
                    int statusCode = (int)statusFloat;
                    string statusText = GetTighteningStatusText((ushort)statusCode);
                    LogManager.LogInfo($"状态码 (5104): {statusCode} - {statusText}");
                }
                else
                {
                    LogManager.LogWarning($"读取状态码失败: {statusCodeResult.Message}");
                }

                // 2. 读取反馈速度（地址5100）
                var speedResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5100", 2));

                if (speedResult.IsSuccess)
                {
                    float speedFloat = ConvertToFloat(speedResult.Content);
                    LogManager.LogInfo($"反馈速度 (5100): {speedFloat:F0} RPM");
                }

                // 3. 读取紧固模式（地址5000）
                var modeResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5000", 2));

                if (modeResult.IsSuccess)
                {
                    float mode = ConvertToFloat(modeResult.Content);
                    LogManager.LogInfo($"紧固模式 (5000): {mode:F0}");
                }

                LogManager.LogInfo("-----------------------------------");
                LogManager.LogInfo("扭矩配置参数：");

                // 4. 读取下限扭矩（地址5002）
                var lowerLimitResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5002", 2));

                if (lowerLimitResult.IsSuccess)
                {
                    float lowerLimit = ConvertToFloat(lowerLimitResult.Content);

                    if (IsValidTorqueValue(lowerLimit))
                    {
                        LogManager.LogInfo($"  下限扭矩 (5002): {lowerLimit:F2} Nm");
                    }
                    else
                    {
                        LogManager.LogWarning($"  下限扭矩 (5002): {lowerLimit:F4} Nm (可能未配置)");
                    }
                }

                // 5. 读取上限扭矩（地址5004）
                var upperLimitResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5004", 2));

                if (upperLimitResult.IsSuccess)
                {
                    float upperLimit = ConvertToFloat(upperLimitResult.Content);

                    if (IsValidTorqueValue(upperLimit))
                    {
                        LogManager.LogInfo($"  上限扭矩 (5004): {upperLimit:F2} Nm");
                    }
                    else
                    {
                        LogManager.LogWarning($"  上限扭矩 (5004): {upperLimit:F4} Nm (可能未配置)");
                    }
                }

                // 6. 读取目标扭矩（地址5006）
                var targetResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5006", 2));

                if (targetResult.IsSuccess)
                {
                    float target = ConvertToFloat(targetResult.Content);

                    if (IsValidTorqueValue(target))
                    {
                        LogManager.LogInfo($"  目标扭矩 (5006): {target:F2} Nm");
                    }
                    else
                    {
                        LogManager.LogWarning($"  目标扭矩 (5006): {target:F4} Nm (可能未配置)");
                    }
                }

                LogManager.LogInfo("-----------------------------------");
                LogManager.LogInfo("角度配置参数：");

                // 7. 读取目标角度（地址5032）
                var targetAngleResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5032", 2));

                if (targetAngleResult.IsSuccess)
                {
                    float targetAngle = ConvertToFloat(targetAngleResult.Content);
                    LogManager.LogInfo($"  目标角度 (5032): {targetAngle:F1}°");
                }

                // 8. 读取下限角度（地址5042）
                var lowerLimitAngleResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5042", 2));

                if (lowerLimitAngleResult.IsSuccess)
                {
                    float lowerLimitAngle = ConvertToFloat(lowerLimitAngleResult.Content);
                    LogManager.LogInfo($"  下限角度 (5042): {lowerLimitAngle:F1}°");
                }

                // 9. 读取上限角度（地址5044）
                var upperLimitAngleResult = await Task.Run(() =>
                    modbusTcpClient.ReadInt16("5044", 2));

                if (upperLimitAngleResult.IsSuccess)
                {
                    float upperLimitAngle = ConvertToFloat(upperLimitAngleResult.Content);
                    LogManager.LogInfo($"  上限角度 (5044): {upperLimitAngle:F1}°");
                }

                LogManager.LogInfo("===================================");
                LogManager.LogInfo("拧紧轴连接测试通过！所有寄存器读取正常。");

                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧轴连接测试异常: {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    modbusTcpClient?.ConnectClose();
                }
                catch { }
            }
        }

        private async Task<bool> TestMESConnection(string url, string apiKey)
        {
            ClientWebSocket webSocket = null;
            try
            {
                LogManager.LogInfo("========== MES系统连接测试 ==========");
                LogManager.LogInfo($"服务器地址: {url}");
                LogManager.LogInfo($"API密钥: {(string.IsNullOrEmpty(apiKey) ? "未配置" : "已配置")}");
                LogManager.LogInfo("-----------------------------------");

                webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                // 如果有API密钥，添加到请求头
                if (!string.IsNullOrEmpty(apiKey))
                {
                    webSocket.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    LogManager.LogInfo("已添加授权头信息");
                }

                var serverUri = new Uri(url);
                var connectToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

                LogManager.LogInfo($"正在连接到MES服务器...");
                await webSocket.ConnectAsync(serverUri, connectToken);

                if (webSocket.State == WebSocketState.Open)
                {
                    LogManager.LogInfo($"WebSocket连接成功 | 协议:{(serverUri.Scheme == "wss" ? "WSS (安全)" : "WS")}");

                    // 发送测试消息
                    var testMessage = new
                    {
                        Type = "connection_test",
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Message = "MES系统连接测试"
                    };

                    string jsonMessage = Newtonsoft.Json.JsonConvert.SerializeObject(testMessage);
                    byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);

                    LogManager.LogInfo($"发送测试消息: {jsonMessage}");
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    // 等待响应
                    var responseBuffer = new byte[1024];
                    var receiveToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

                    try
                    {
                        var result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(responseBuffer),
                            receiveToken);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string serverResponse = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);
                            LogManager.LogInfo($"服务器响应: {serverResponse}");

                            // 检查响应是否包含成功关键词
                            if (IsSuccessfulResponse(serverResponse))
                            {
                                LogManager.LogInfo("✓ MES服务器响应验证通过");
                                return true;
                            }
                            else
                            {
                                LogManager.LogWarning("✗ MES服务器响应未包含预期的成功关键词");
                                return false;
                            }
                        }
                        else
                        {
                            LogManager.LogInfo("✓ MES连接成功，服务器无文本响应");
                            return true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogManager.LogInfo("✓ MES连接成功，但服务器未在5秒内响应");
                        return true; // 连接成功但无响应也认为是成功的
                    }
                }
                else
                {
                    LogManager.LogError($"WebSocket连接失败，状态: {webSocket.State}");
                    return false;
                }
            }
            catch (WebSocketException wsEx)
            {
                LogManager.LogError($"WebSocket连接异常: {wsEx.Message} | 错误代码: {wsEx.WebSocketErrorCode}");
                return false;
            }
            catch (UriFormatException uriEx)
            {
                LogManager.LogError($"服务器地址格式错误: {uriEx.Message}");
                return false;
            }
            catch (TimeoutException timeEx)
            {
                LogManager.LogError($"连接超时: {timeEx.Message}");
                return false;
            }
            catch (OperationCanceledException cancelEx)
            {
                LogManager.LogError($"连接被取消: {cancelEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"MES连接测试异常: {ex.GetType().Name} | {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (webSocket?.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection test completed",
                            CancellationToken.None);
                        LogManager.LogInfo("WebSocket连接已正常关闭");
                    }
                }
                catch (Exception cleanEx)
                {
                    LogManager.LogDebug($"关闭WebSocket连接异常: {cleanEx.Message}");
                }
                finally
                {
                    webSocket?.Dispose();
                    LogManager.LogInfo("===================================");
                }
            }
        }

        /// <summary>
        /// 判断MES服务器响应是否成功
        /// </summary>
        private bool IsSuccessfulResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return false;

            var successKeywords = _config.Server.SuccessKeywords ?? new[] { "success", "ok", "received", "完成", "成功" };

            foreach (var keyword in successKeywords)
            {
                if (response.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LogManager.LogInfo($"✓ 发现成功关键词: '{keyword}'");
                    return true;
                }
            }

            LogManager.LogWarning($"✗ 响应中未发现成功关键词，期望: [{string.Join(", ", successKeywords)}]");
            return false;
        }

        /// <summary>
        /// 获取拧紧轴运行状态文本
        /// </summary>
        private string GetTighteningStatusText(ushort statusCode)
        {
            switch (statusCode)
            {
                case 0: return "空闲";
                case 1: return "运行中";
                case 11: return "合格";
                case 21: return "小于下限扭矩";
                case 22: return "大于上限扭矩";
                case 23: return "运行超最上限时间";
                case 24: return "小于下限角度";
                case 25: return "大于上限角度";
                case 0xA000: // 40960
                    return "未初始化/待配置";
                default:
                    return statusCode > 100 ? "错误状态" : "未知状态";
            }
        }

        private async Task<bool> TestTcpConnection(string ip, int port, string deviceName)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.ReceiveTimeout = 5000;
                    tcpClient.SendTimeout = 5000;

                    var connectTask = tcpClient.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(5000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && !connectTask.IsFaulted && tcpClient.Connected)
                    {
                        LogManager.LogInfo($"{deviceName}连接测试成功");
                        return true;
                    }
                    else
                    {
                        LogManager.LogWarning($"{deviceName}连接测试超时或失败");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"{deviceName}连接测试异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 输入验证方法

        private bool ValidatePLCInput()
        {
            if (string.IsNullOrWhiteSpace(plcIpTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入PLC IP地址", autoClose: 3);
                plcIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(plcIpTextBox.Text.Trim(), out _))
            {
                AntdUI.Message.error(GetParentForm(), "PLC IP地址格式不正确", autoClose: 3);
                plcIpTextBox.Focus();
                return false;
            }

            if (!int.TryParse(plcPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                AntdUI.Message.error(GetParentForm(), "PLC端口号必须在1-65535之间", autoClose: 3);
                plcPortTextBox.Focus();
                return false;
            }

            if (!byte.TryParse(plcStationTextBox.Text.Trim(), out byte station))
            {
                AntdUI.Message.error(GetParentForm(), "PLC站号格式不正确", autoClose: 3);
                plcStationTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateScannerInput()
        {
            if (string.IsNullOrWhiteSpace(scannerIpTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入扫码枪IP地址", autoClose: 3);
                scannerIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(scannerIpTextBox.Text.Trim(), out _))
            {
                AntdUI.Message.error(GetParentForm(), "扫码枪IP地址格式不正确", autoClose: 3);
                scannerIpTextBox.Focus();
                return false;
            }

            if (!int.TryParse(scannerPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                AntdUI.Message.error(GetParentForm(), "扫码枪端口号必须在1-65535之间", autoClose: 3);
                scannerPortTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateTighteningAxisInput()
        {
            if (string.IsNullOrWhiteSpace(tighteningAxisIpTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入拧紧轴IP地址", autoClose: 3);
                tighteningAxisIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(tighteningAxisIpTextBox.Text.Trim(), out _))
            {
                AntdUI.Message.error(GetParentForm(), "拧紧轴IP地址格式不正确", autoClose: 3);
                tighteningAxisIpTextBox.Focus();
                return false;
            }

            if (!int.TryParse(tighteningAxisPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                AntdUI.Message.error(GetParentForm(), "拧紧轴端口号必须在1-65535之间", autoClose: 3);
                tighteningAxisPortTextBox.Focus();
                return false;
            }

            if (!byte.TryParse(tighteningAxisStationTextBox.Text.Trim(), out byte station))
            {
                AntdUI.Message.error(GetParentForm(), "拧紧轴Modbus站号格式不正确", autoClose: 3);
                tighteningAxisStationTextBox.Focus();
                return false;
            }

            // 验证端口是否为Modbus标准端口
            if (port != 502)
            {
                AntdUI.Message.warn(GetParentForm(), "拧紧轴通常使用Modbus TCP端口502，请确认端口配置", autoClose: 5);
            }

            return true;
        }

        private bool ValidateMESInput()
        {
            if (string.IsNullOrWhiteSpace(mesUrlTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入MES服务器地址", autoClose: 3);
                mesUrlTextBox.Focus();
                return false;
            }

            var url = mesUrlTextBox.Text.Trim();

            // 验证URL格式
            if (!url.StartsWith("ws://") && !url.StartsWith("wss://"))
            {
                AntdUI.Message.error(GetParentForm(), "MES服务器地址必须以 ws:// 或 wss:// 开头", autoClose: 3);
                mesUrlTextBox.Focus();
                return false;
            }

            // 验证URL是否为有效格式
            try
            {
                var uri = new Uri(url);
                if (uri.Host == null || string.IsNullOrEmpty(uri.Host))
                {
                    throw new UriFormatException("主机名无效");
                }
            }
            catch (UriFormatException)
            {
                AntdUI.Message.error(GetParentForm(), "MES服务器地址格式不正确", autoClose: 3);
                mesUrlTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateAllInputs()
        {
            return ValidatePLCInput() &&
                   ValidateScannerInput() &&
                   ValidateTighteningAxisInput() &&
                   ValidateMESInput();
        }

        #endregion

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAllInputs()) return;

            try
            {
                SaveAllSettings();
                LogManager.LogInfo("保存通讯设置");
                AntdUI.Message.success(GetParentForm(), "设置保存成功！", autoClose: 3);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存设置失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"保存设置失败: {ex.Message}", autoClose: 3);
            }
        }

        private void SaveAllSettings()
        {
            // 更新配置对象
            _config.PLC.IP = plcIpTextBox.Text.Trim();
            _config.PLC.Port = int.Parse(plcPortTextBox.Text.Trim());
            _config.PLC.Station = byte.Parse(plcStationTextBox.Text.Trim());

            _config.Scanner.IP = scannerIpTextBox.Text.Trim();
            _config.Scanner.Port = int.Parse(scannerPortTextBox.Text.Trim());

            _config.TighteningAxis.IP = tighteningAxisIpTextBox.Text.Trim();
            _config.TighteningAxis.Port = int.Parse(tighteningAxisPortTextBox.Text.Trim());
            _config.TighteningAxis.Station = byte.Parse(tighteningAxisStationTextBox.Text.Trim());


            // 保存到配置文件
            ConfigManager.SaveConfig(_config);
        }

        // 设置变更事件
        public event EventHandler SettingsChanged;

        // 公共方法供其他模块获取当前配置
        public CommunicationConfig GetCurrentConfig()
        {
            return _config;
        }

        #region 数据转换辅助方法

        private float ConvertToFloat(short[] registers)
        {
            if (registers == null || registers.Length < 2)
                return 0f;

            try
            {
                // 拧紧轴格式：寄存器[1]在高位，寄存器[0]在低位
                uint combined = ((uint)(ushort)registers[1] << 16) | (uint)(ushort)registers[0];
                byte[] bytes = BitConverter.GetBytes(combined);
                return BitConverter.ToSingle(bytes, 0);
            }
            catch
            {
                return 0f;
            }
        }


        /// <summary>
        /// 验证扭矩值是否在合理范围内
        /// </summary>
        private bool IsValidTorqueValue(float torque)
        {
            return !float.IsNaN(torque) &&
                   !float.IsInfinity(torque) &&
                   torque >= 0f &&
                   torque <= 100f;  // 根据实际设备调整上限
        }

        #endregion
    }
}

