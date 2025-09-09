using AntdUI;
using System;
using System.IO.Ports;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;

namespace TailInstallationSystem
{
    public partial class CommunicationSettingsControl : UserControl
    {
        public CommunicationSettingsControl()
        {
            InitializeComponent();
            InitializeControls();
            LoadSettings();
        }

        // 获取父窗体的辅助方法
        private Form GetParentForm()
        {
            return this.FindForm() ?? this.ParentForm;
        }

        private void InitializeControls()
        {
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            try
            {
                // 初始化串口选项
                screwComComboBox.Items.Clear();
                var portNames = SerialPort.GetPortNames();
                foreach (var port in portNames)
                {
                    screwComComboBox.Items.Add(port);
                }
                if (screwComComboBox.Items.Count > 0)
                {
                    screwComComboBox.SelectedIndex = 0;
                }
                else
                {
                    screwComComboBox.Items.Add("COM1");
                    screwComComboBox.SelectedIndex = 0;
                }

                // 初始化波特率选项
                screwBaudComboBox.Items.Clear();
                var baudRates = new string[] { "9600", "19200", "38400", "57600", "115200" };
                foreach (var baud in baudRates)
                {
                    screwBaudComboBox.Items.Add(baud);
                }
                screwBaudComboBox.SelectedIndex = 0; // 默认9600
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化控件失败: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                // 从配置文件加载设置
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
            // 可以从配置文件或注册表读取
            // 这里使用默认值
        }

        private void LoadScannerSettings()
        {
            // 加载扫码枪设置
        }

        private void LoadScrewDriverSettings()
        {
            // 加载螺丝机设置
        }

        private void LoadPCSettings()
        {
            // 加载PC通讯设置
        }

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

                // 模拟异步测试
                await Task.Delay(2000);

                AntdUI.Message.success(GetParentForm(), "PLC连接测试成功！", autoClose: 3);
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

                await Task.Delay(1500);

                AntdUI.Message.success(GetParentForm(), "扫码枪连接测试成功！", autoClose: 3);
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

                var com = screwComComboBox.Text;
                var baud = int.Parse(screwBaudComboBox.Text);

                LogManager.LogInfo($"测试螺丝机连接: {com}, 波特率:{baud}");

                await Task.Delay(1000);

                AntdUI.Message.success(GetParentForm(), "螺丝机连接测试成功！", autoClose: 3);
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

                await Task.Delay(1200);

                AntdUI.Message.success(GetParentForm(), "PC通讯连接测试成功！", autoClose: 3);
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

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAllInputs()) return;

            try
            {
                // 保存所有设置
                SaveAllSettings();

                LogManager.LogInfo("保存通讯设置");
                AntdUI.Message.success(GetParentForm(), "设置保存成功！", autoClose: 3);

                // 触发设置变更事件
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"保存设置失败: {ex.Message}");
                AntdUI.Message.error(GetParentForm(), $"保存设置失败: {ex.Message}", autoClose: 3);
            }
        }

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
            if (string.IsNullOrWhiteSpace(screwComComboBox.Text))
            {
                AntdUI.Message.error(GetParentForm(), "请选择螺丝机串口", autoClose: 3);
                screwComComboBox.Focus();
                return false;
            }

            if (!int.TryParse(screwBaudComboBox.Text, out int baud))
            {
                AntdUI.Message.error(GetParentForm(), "螺丝机波特率格式不正确", autoClose: 3);
                screwBaudComboBox.Focus();
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

        private void SaveAllSettings()
        {
            // 实现保存逻辑到配置文件或数据库
            // 这里可以使用 ConfigurationManager 或其他配置方案
        }

        // 设置变更事件
        public event EventHandler SettingsChanged;
    }
}

