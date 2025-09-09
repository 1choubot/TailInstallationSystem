using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace TailInstallationSystem
{
    public partial class DataViewControl : UserControl
    {
        private DataManager dataManager;
        private List<ProductDataViewModel> allData;
        private BindingList<ProductDataViewModel> displayData;

        public DataViewControl()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            try
            {
                dataManager = new DataManager();
                allData = new List<ProductDataViewModel>();
                displayData = new BindingList<ProductDataViewModel>();

                // 设置数据源
                dataGridView.DataSource = displayData;

                // 配置DataGridView样式
                ConfigureDataGridView();

                LoadData();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"初始化数据视图控件失败: {ex.Message}");
                ShowMessage("初始化失败", MessageType.Error);
            }
        }

        private void ConfigureDataGridView()
        {
            // 配置列宽自适应
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 配置行高
            dataGridView.RowTemplate.Height = 35;

            // 配置表头样式
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            dataGridView.ColumnHeadersHeight = 40;

            // 配置奇偶行颜色
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 248, 248);

            // 配置选中行样式
            dataGridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(51, 153, 255);
            dataGridView.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
        }

        private async void LoadData()
        {
            try
            {
                SetButtonLoadingState(refreshButton, true, "加载中...");

                var productDataList = await dataManager.GetProductDataHistory(30);

                allData = productDataList.Select(p => new ProductDataViewModel
                {
                    Id = p.Id,
                    Barcode = p.Barcode ?? "N/A",
                    Status = (p.IsCompleted ?? false) ? "已完成" : "进行中", // 处理 nullable bool
                    CreatedTime = p.CreatedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    CompletedTime = p.CompletedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    IsUploaded = (p.IsUploaded ?? false) ? "已上传" : "未上传", // 处理 nullable bool
                    UploadedTime = p.UploadedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    OriginalData = p
                }).ToList();

                RefreshDisplayData(allData);

                LogManager.LogInfo($"加载了 {productDataList.Count} 条生产数据");
                UpdateStatusInfo();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"加载数据失败: {ex.Message}");
                ShowMessage("加载数据失败！", MessageType.Error);
            }
            finally
            {
                SetButtonLoadingState(refreshButton, false, "🔄 刷新");
            }
        }

        private void RefreshDisplayData(List<ProductDataViewModel> data)
        {
            displayData.Clear();
            foreach (var item in data)
            {
                displayData.Add(item);
            }
        }

        private void UpdateStatusInfo()
        {
            var totalCount = allData.Count;
            var completedCount = allData.Count(d => d.Status == "已完成");
            var uploadedCount = allData.Count(d => d.IsUploaded == "已上传");

            LogManager.LogInfo($"数据统计 - 总计:{totalCount}, 已完成:{completedCount}, 已上传:{uploadedCount}");
        }

        #region 辅助方法

        /// <summary>
        /// 设置按钮加载状态
        /// </summary>
        private void SetButtonLoadingState(AntdUI.Button button, bool loading, string text)
        {
            if (button == null) return;

            button.Loading = loading;
            button.Text = text;
            button.Enabled = !loading;
        }

        /// <summary>
        /// 显示消息 - 统一消息显示方法
        /// </summary>
        private void ShowMessage(string message, MessageType type)
        {
            try
            {
                var parentForm = this.FindForm();
                if (parentForm != null)
                {
                    // 使用 AntdUI.Message
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
                    // 如果找不到父窗体，使用传统的 MessageBox
                    MessageBoxIcon icon = MessageBoxIcon.Information;
                    switch (type)
                    {
                        case MessageType.Success:
                            icon = MessageBoxIcon.Information;
                            break;
                        case MessageType.Error:
                            icon = MessageBoxIcon.Error;
                            break;
                        case MessageType.Warning:
                            icon = MessageBoxIcon.Warning;
                            break;
                    }
                    MessageBox.Show(message, "提示", MessageBoxButtons.OK, icon);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"显示消息失败: {ex.Message}");
                // 最后的备用方案
                MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region 事件处理方法

        private void refreshButton_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private async void exportButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (displayData.Count == 0)
                {
                    ShowMessage("没有数据可导出", MessageType.Warning);
                    return;
                }

                SetButtonLoadingState(exportButton, true, "导出中...");

                await ExportData();

                LogManager.LogInfo("导出生产数据成功");
                ShowMessage("数据导出成功！", MessageType.Success);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"导出数据失败: {ex.Message}");
                ShowMessage("导出失败！", MessageType.Error);
            }
            finally
            {
                SetButtonLoadingState(exportButton, false, "📤 导出");
            }
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var searchText = searchTextBox.Text?.Trim().ToLower();

                if (string.IsNullOrEmpty(searchText))
                {
                    RefreshDisplayData(allData);
                    return;
                }

                var filteredData = allData.Where(item =>
                    (item.Barcode?.ToLower().Contains(searchText) ?? false) ||
                    (item.Status?.ToLower().Contains(searchText) ?? false) ||
                    (item.IsUploaded?.ToLower().Contains(searchText) ?? false)
                ).ToList();

                RefreshDisplayData(filteredData);

                LogManager.LogInfo($"搜索: {searchText}, 找到 {filteredData.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"搜索失败: {ex.Message}");
            }
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == actionsColumn.Index && e.RowIndex >= 0 && e.RowIndex < displayData.Count)
                {
                    var selectedItem = displayData[e.RowIndex];
                    ShowProductDetails(selectedItem);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理单元格点击事件失败: {ex.Message}");
                ShowMessage("操作失败", MessageType.Error);
            }
        }

        #endregion

        #region 数据操作方法

        private async Task ExportData()
        {
            await Task.Run(() =>
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        var saveFileDialog = new SaveFileDialog
                        {
                            Filter = "CSV文件 (*.csv)|*.csv|Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                            FileName = $"生产数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                        };

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            var extension = Path.GetExtension(saveFileDialog.FileName).ToLower();
                            switch (extension)
                            {
                                case ".csv":
                                    ExportToCSV(saveFileDialog.FileName);
                                    break;
                                case ".xlsx":
                                    ExportToExcel(saveFileDialog.FileName);
                                    break;
                                default:
                                    ExportToCSV(saveFileDialog.FileName);
                                    break;
                            }
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"导出操作失败: {ex.Message}");
                    throw;
                }
            });
        }

        private void ExportToCSV(string fileName)
        {
            try
            {
                var csv = new StringBuilder();
                // 添加 BOM 以支持中文
                csv.Append('\uFEFF');
                csv.AppendLine("产品条码,状态,创建时间,完成时间,上传状态,上传时间");

                foreach (var item in displayData)
                {
                    csv.AppendLine($"{EscapeCsvField(item.Barcode)},{EscapeCsvField(item.Status)},{EscapeCsvField(item.CreatedTime)},{EscapeCsvField(item.CompletedTime)},{EscapeCsvField(item.IsUploaded)},{EscapeCsvField(item.UploadedTime)}");
                }

                File.WriteAllText(fileName, csv.ToString(), new UTF8Encoding(true));
                LogManager.LogInfo($"数据已导出到CSV: {fileName}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"CSV导出失败: {ex.Message}");
                throw;
            }
        }

        private void ExportToExcel(string fileName)
        {
            try
            {
                // 如果需要真正的Excel导出，建议使用 EPPlus 或 NPOI
                // 这里暂时转为CSV格式
                var csvFileName = Path.ChangeExtension(fileName, ".csv");
                ExportToCSV(csvFileName);
                LogManager.LogInfo($"数据已导出为CSV格式: {csvFileName}");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Excel导出失败: {ex.Message}");
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // 如果字段包含逗号、引号或换行符，需要用引号包围
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                // 将字段中的引号转义为两个引号
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }

            return field;
        }

        private void ShowProductDetails(ProductDataViewModel productData)
        {
            try
            {
                LogManager.LogInfo($"查看产品详情: {productData.Barcode}");

                var detailInfo = $@"产品条码: {productData.Barcode}
状态: {productData.Status}
创建时间: {productData.CreatedTime}
完成时间: {productData.CompletedTime}
上传状态: {productData.IsUploaded}
上传时间: {productData.UploadedTime}

详细数据:
工序1数据: {productData.OriginalData?.Process1_Data ?? "N/A"}
工序2数据: {productData.OriginalData?.Process2_Data ?? "N/A"}
工序3数据: {productData.OriginalData?.Process3_Data ?? "N/A"}
尾夹工序数据: {productData.OriginalData?.Process4_Data ?? "N/A"}
完成数据: {productData.OriginalData?.CompleteData ?? "N/A"}";

                MessageBox.Show(detailInfo, $"产品详情 - {productData.Barcode}",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"显示产品详情失败: {ex.Message}");
                ShowMessage("显示详情失败", MessageType.Error);
            }
        }

        #endregion

        // 公共方法，供外部调用刷新数据
        public void RefreshData()
        {
            LoadData();
        }
    }

    // 消息类型枚举
    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }

    // 数据视图模型
    public class ProductDataViewModel
    {
        public long Id { get; set; }
        public string Barcode { get; set; }
        public string Status { get; set; }
        public string CreatedTime { get; set; }
        public string CompletedTime { get; set; }
        public string IsUploaded { get; set; }
        public string UploadedTime { get; set; }
        public ProductData OriginalData { get; set; }
    }
}
