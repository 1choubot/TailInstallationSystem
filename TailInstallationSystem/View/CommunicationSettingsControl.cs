using AntdUI;
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
                LoadScrewDriverSettings();
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

        private void LoadScrewDriverSettings()
        {
            screwIpTextBox.Text = _config.ScrewDriver.IP;
            screwPortTextBox.Text = _config.ScrewDriver.Port.ToString();
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

        private async void screwTestButton_Click(object sender, EventArgs e)
        {
            if (!ValidateScrewDriverInput()) return;

            try
            {
                screwTestButton.Loading = true;
                screwTestButton.Text = "测试中...";
                screwTestButton.Enabled = false;

                var ip = screwIpTextBox.Text.Trim();
                var port = int.Parse(screwPortTextBox.Text.Trim());

                LogManager.LogInfo($"测试螺丝机连接: {ip}:{port}");

                bool success = await TestTcpConnection(ip, port, "螺丝机");

                if (success)
                {
                    AntdUI.Message.success(GetParentForm(), "螺丝机连接测试成功！", autoClose: 3);
                }
                else
                {
                    AntdUI.Message.error(GetParentForm(), "螺丝机连接测试失败！", autoClose: 3);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"螺丝机连接测试失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"螺丝机连接测试失败: {ex.Message}", autoClose: 3);
            }
            finally
            {
                screwTestButton.Loading = false;
                screwTestButton.Text = "测试连接";
                screwTestButton.Enabled = true;
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

                bool success = await TestTcpConnection(ip, port, "PC");

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
                    // 尝试读取一个地址测试连接
                    var readResult = await Task.Run(() => modbusTcpClient.ReadBool("M100"));
                    modbusTcpClient.ConnectClose();
                    return readResult.IsSuccess;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError($"PLC连接测试异常: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestTcpConnection(string ip, int port, string deviceName)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // 设置连接超时
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

        private bool ValidateScrewDriverInput()
        {
            if (string.IsNullOrWhiteSpace(screwIpTextBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请输入螺丝机IP地址", autoClose: 3);
                screwIpTextBox.Focus();
                return false;
            }

            if (!IPAddress.TryParse(screwIpTextBox.Text.Trim(), out _))
            {
                AntdUI.Message.error(GetParentForm(), "螺丝机IP地址格式不正确", autoClose: 3);
                screwIpTextBox.Focus();
                return false;
            }

            if (!int.TryParse(screwPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                AntdUI.Message.error(GetParentForm(), "螺丝机端口号必须在1-65535之间", autoClose: 3);
                screwPortTextBox.Focus();
                return false;
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
                   ValidateScrewDriverInput() &&
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

            _config.ScrewDriver.IP = screwIpTextBox.Text.Trim();
            _config.ScrewDriver.Port = int.Parse(screwPortTextBox.Text.Trim());

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
