﻿using AntdUI;
using System;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
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
            pcTestButton.Click += pcTestButton_Click;
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
                LoadPCSettings();

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

        private void LoadPCSettings()
        {
            pcIpTextBox.Text = _config.PC.IP;
            pcPortTextBox.Text = _config.PC.Port.ToString();
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

        private async void pcTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidatePCInput()) return;
            try
            {
                pcTestButton.Loading = true;
                pcTestButton.Text = "测试中...";
                pcTestButton.Enabled = false;

                var ip = pcIpTextBox.Text.Trim();
                var port = int.Parse(pcPortTextBox.Text.Trim());

                LogManager.LogInfo($"测试PC通讯连接: {ip}:{port}");

                // 检查PC配置模式 - 默认是服务端模式
                bool success = await TestPCServerMode(port);

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "PC通讯连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "PC通讯连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PC通讯连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"PC通讯连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                pcTestButton.Loading = false;
                pcTestButton.Text = "测试连接";
                pcTestButton.Enabled = true;
            }
        }

        /// <summary>
        /// 测试PC服务端模式 - 检查端口是否可用
        /// </summary>
        private async Task<bool> TestPCServerMode(int port)
        {
            TcpListener testListener = null;
            TcpClient testClient = null;
            
            try
            {
                LogManager.LogInfo($"测试PC服务端模式，端口: {port}");
                
                // 1. 尝试启动服务端
                testListener = new TcpListener(IPAddress.Any, port);
                testListener.Start();
                LogManager.LogInfo($"PC服务端启动成功，端口: {port}");
                
                // 2. 尝试客户端连接测试
                testClient = new TcpClient();
                var connectTask = testClient.ConnectAsync("127.0.0.1", port);
                var timeoutTask = Task.Delay(3000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && !connectTask.IsFaulted && testClient.Connected)
                {
                    LogManager.LogInfo("PC服务端连接测试成功 - 可以正常接受客户端连接");
                    return true;
                }
                else
                {
                    LogManager.LogWarning("PC服务端连接测试失败 - 无法接受客户端连接");
                    return false;
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // 端口被占用，需要进一步检查是否是系统占用
                    LogManager.LogWarning($"PC服务端端口 {port} 已被占用");
                    
                    // 尝试连接到已存在的服务端
                    try
                    {
                        testClient = new TcpClient();
                        await testClient.ConnectAsync("127.0.0.1", port);
                        if (testClient.Connected)
                        {
                            LogManager.LogInfo("端口已被占用，但可以正常连接 - 可能系统已在运行");
                            return true;
                        }
                    }
                    catch
                    {
                        LogManager.LogError("端口被占用且无法连接 - 可能被其他程序占用");
                        return false;
                    }
                }
                
                LogManager.LogError($"PC服务端端口测试失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PC服务端模式测试异常: {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    testClient?.Close();
                    testListener?.Stop();
                }
                catch (Exception ex)
                {
                    LogManager.LogWarning($"清理测试资源时异常: {ex.Message}");
                }
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
                    string testAddress = _config.PLC.TriggerAddress.Replace("D", "");
                    var readResult = await Task.Run(() => modbusTcpClient.ReadInt16(testAddress, 1));
                    modbusTcpClient.ConnectClose();
                    
                    if (readResult.IsSuccess)
                    {
                        LogManager.LogInfo($"PLC测试成功，寄存器100值: {readResult.Content[0]}");
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
            try
            {
                var modbusTcpClient = new ModbusTcpNet(ip, port, station);
                var connectResult = await Task.Run(() => modbusTcpClient.ConnectServer());

                if (connectResult.IsSuccess)
                {
                    // 尝试读取拧紧轴的运行状态寄存器来测试连接
                    var readResult = await Task.Run(() => modbusTcpClient.ReadInt16("5100", 1));
                    modbusTcpClient.ConnectClose();

                    if (readResult.IsSuccess)
                    {
                        LogManager.LogInfo($"拧紧轴测试读取成功，运行状态值: {readResult.Content[0]}");
                        return true;
                    }
                    else
                    {
                        LogManager.LogWarning($"拧紧轴连接成功但读取数据失败: {readResult.Message}");
                        return false;
                    }
                }
                else
                {
                    LogManager.LogWarning($"拧紧轴Modbus连接失败: {connectResult.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"拧紧轴连接测试异常: {ex.Message}");
                return false;
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

        private bool ValidatePCInput()
        {
            if (string.IsNullOrWhiteSpace(pcIpTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入PC IP地址", autoClose: 3);
                pcIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(pcIpTextBox.Text.Trim(), out _))
            {
                AntdUI.Message.error(GetParentForm(), "PC IP地址格式不正确", autoClose: 3);
                pcIpTextBox.Focus();
                return false;
            }

            if (!int.TryParse(pcPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                AntdUI.Message.error(GetParentForm(), "PC端口号必须在1-65535之间", autoClose: 3);
                pcPortTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool ValidateAllInputs()
        {
            return ValidatePLCInput() &&
                   ValidateScannerInput() &&
                   ValidateTighteningAxisInput() &&
                   ValidatePCInput();
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

            _config.PC.IP = pcIpTextBox.Text.Trim();
            _config.PC.Port = int.Parse(pcPortTextBox.Text.Trim());

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
    }
}
