using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace TailInstallationSystem
{
    public partial class DataViewControl : UserControl
    {
        private DataService dataService;
        private List<ProductDataViewModel> allData;
        private BindingList<ProductDataViewModel> displayData;

        public DataViewControl()
        {
            InitializeComponent();
            InitializeControls();
            dataGridView.CellPainting += DataGridView_CellPainting;

        }

        private void InitializeControls()
        {
            try
            {
                dataService = new DataService();
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

                var productDataList = await dataService.GetProductDataHistory(30);
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
                SetButtonLoadingState(refreshButton, false, "刷新");
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

            button.Enabled = !loading;
            button.Loading = loading;
            button.Text = text;
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

                bool exportResult = await ExportData();

                if (exportResult)
                {
                    LogManager.LogInfo("导出生产数据成功");
                    ShowMessage("数据导出成功！", MessageType.Success);
                }
                else
                {
                    LogManager.LogInfo("用户取消导出操作");
                    ShowMessage("导出已取消", MessageType.Info);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"导出数据失败: {ex.Message}");
                ShowMessage("导出失败！", MessageType.Error);
            }
            finally
            {
                SetButtonLoadingState(exportButton, false, "导出");
            }
        }

        // 筛选事件处理方法
        private void uploadStatusComboBox_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            try
            {
                var searchText = searchTextBox.Text?.Trim().ToLower() ?? "";
                var selectedUploadStatus = uploadStatusComboBox.SelectedValue?.ToString() ?? "全部数据";

                var filteredData = allData.Where(item =>
                {
                    // 文本搜索筛选
                    bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                        (item.Barcode?.ToLower().Contains(searchText) ?? false) ||
                        (item.Status?.ToLower().Contains(searchText) ?? false);

                    // 上传状态筛选
                    bool matchesUploadStatus = selectedUploadStatus == "全部数据" ||
                        (selectedUploadStatus == "已上传" && item.IsUploaded == "已上传") ||
                        (selectedUploadStatus == "未上传" && item.IsUploaded == "未上传");

                    return matchesSearch && matchesUploadStatus;
                }).ToList();

                RefreshDisplayData(filteredData);

                LogManager.LogInfo($"筛选完成，显示 {filteredData.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"筛选失败: {ex.Message}");
            }
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyCurrentFilter(); // 替换原有的筛选逻辑
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == actionsColumn.Index && e.RowIndex >= 0 && e.RowIndex < displayData.Count)
                {
                    var selectedItem = displayData[e.RowIndex];

                    if (selectedItem.IsUploaded == "已上传")
                    {
                        // 只有查看详情功能
                        ShowProductDetails(selectedItem);
                    }
                    else
                    {
                        // 有两个按钮，需要判断点击了哪个
                        var clickedButton = GetClickedButtonIndex(e);

                        if (clickedButton == 0)
                        {
                            // 点击了第一个按钮：查看详情
                            ShowProductDetails(selectedItem);
                        }
                        else if (clickedButton == 1)
                        {
                            // 点击了第二个按钮：手动上传
                            HandleManualUpload(selectedItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"处理单元格点击事件失败: {ex.Message}");
                ShowMessage("操作失败", MessageType.Error);
            }
        }

        // 辅助方法
        private int GetClickedButtonIndex(DataGridViewCellEventArgs e)
        {
            var cellBounds = dataGridView.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            var mousePos = dataGridView.PointToClient(Cursor.Position);

            // 计算相对于单元格的点击位置
            var relativeX = mousePos.X - cellBounds.Left;
            var buttonWidth = (cellBounds.Width - 30) / 2;

            if (relativeX < buttonWidth + 10)
            {
                return 0; // 第一个按钮
            }
            else
            {
                return 1; // 第二个按钮
            }
        }

        // 手动上传处理方法
        private async void HandleManualUpload(ProductDataViewModel item)
        {
            try
            {
                var result = MessageBox.Show(
                    $"确定要手动上传产品 '{item.Barcode}' 的数据吗？",
                    "确认上传",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                ShowMessage("正在上传数据...", MessageType.Info);
                LogManager.LogInfo($"开始手动上传数据: {item.Barcode}");

                using (var tempDataManager = new DataManager())
                {
                    bool success = await tempDataManager.UploadToServer(item.Barcode, item.OriginalData.CompleteData);

                    if (success)
                    {
                        ShowMessage("数据上传成功！", MessageType.Success);
                        LogManager.LogInfo($"手动上传成功: {item.Barcode}");
                        LoadData(); // 刷新数据显示
                    }
                    else
                    {
                        ShowMessage("数据上传失败，请检查网络连接或服务器状态", MessageType.Warning);
                        LogManager.LogWarning($"手动上传失败: {item.Barcode}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"手动上传异常: {ex.Message}, 条码: {item.Barcode}");
                ShowMessage($"上传异常: {ex.Message}", MessageType.Error);
            }
        }


        #endregion

        #region 数据操作方法

        private async Task<bool> ExportData()
        {
            return await Task.Run(() =>
            {
                bool result = false;
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
                            result = true;
                        }
                        // 如果用户点击取消，result保持为false
                    }));
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"导出操作失败: {ex.Message}");
                    throw; // 重新抛出异常
                }
                return result;
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

                // 使用 AntdUI 的详情窗口替代 MessageBox
                using (var detailForm = new Forms.ProductDetailForm(productData))
                {
                    detailForm.ShowDialog(this.FindForm());
                }
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

        private void DataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex == actionsColumn.Index && e.RowIndex >= 0)
            {
                e.Handled = true;
                e.PaintBackground(e.CellBounds, true);
                var item = displayData[e.RowIndex];

                if (item.IsUploaded == "已上传")
                {
                    // 只显示查看详情按钮
                    DrawSingleButton(e.Graphics, e.CellBounds, "查看详情", Color.FromArgb(51, 153, 255));
                }
                else
                {
                    // 显示两个按钮：查看详情 和 手动上传
                    DrawDoubleButtons(e.Graphics, e.CellBounds, "查看详情", "手动上传");
                }
            }
        }
        private void DrawSingleButton(Graphics graphics, Rectangle cellBounds, string text, Color buttonColor)
        {
            var buttonRect = new Rectangle(
                cellBounds.Left + 10,
                cellBounds.Top + 6,
                cellBounds.Width - 20,
                cellBounds.Height - 12
            );

            DrawButton(graphics, buttonRect, text, buttonColor);
        }
        private void DrawDoubleButtons(Graphics graphics, Rectangle cellBounds, string text1, string text2)
        {
            int buttonWidth = (cellBounds.Width - 30) / 2;

            // 第一个按钮：查看详情
            var button1Rect = new Rectangle(
                cellBounds.Left + 5,
                cellBounds.Top + 6,
                buttonWidth,
                cellBounds.Height - 12
            );
            DrawButton(graphics, button1Rect, text1, Color.FromArgb(51, 153, 255));

            // 第二个按钮：手动上传
            var button2Rect = new Rectangle(
                cellBounds.Left + buttonWidth + 15,
                cellBounds.Top + 6,
                buttonWidth,
                cellBounds.Height - 12
            );
            DrawButton(graphics, button2Rect, text2, Color.FromArgb(34, 139, 34));
        }
        private void DrawButton(Graphics graphics, Rectangle buttonRect, string text, Color buttonColor)
        {
            var borderRadius = 6;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(buttonRect.Left, buttonRect.Top, borderRadius, borderRadius, 180, 90);
                path.AddArc(buttonRect.Right - borderRadius, buttonRect.Top, borderRadius, borderRadius, 270, 90);
                path.AddArc(buttonRect.Right - borderRadius, buttonRect.Bottom - borderRadius, borderRadius, borderRadius, 0, 90);
                path.AddArc(buttonRect.Left, buttonRect.Bottom - borderRadius, borderRadius, borderRadius, 90, 90);
                path.CloseFigure();
                using (SolidBrush brush = new SolidBrush(buttonColor))
                {
                    graphics.FillPath(brush, path);
                }
            }
            // 绘制文字
            using (Font font = new Font("微软雅黑", 8F, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                var textSize = graphics.MeasureString(text, font);
                var textX = buttonRect.Left + (buttonRect.Width - textSize.Width) / 2;
                var textY = buttonRect.Top + (buttonRect.Height - textSize.Height) / 2;
                graphics.DrawString(text, font, textBrush, textX, textY);
            }
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