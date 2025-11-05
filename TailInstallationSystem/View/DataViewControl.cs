using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using static TailInstallationSystem.View.SystemLogControl;

namespace TailInstallationSystem
{
    public partial class DataViewControl : UserControl
    {
        private DataService dataService;
        private List<ProductDataViewModel> allData;
        private BindingList<ProductDataViewModel> displayData;
        private bool isBatchUploading = false;

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

            // 配置质量状态列颜色
            dataGridView.CellFormatting += DataGridView_CellFormatting;
        }

        /// <summary>
        /// 单元格格式化事件（用于设置质量状态颜色）
        /// </summary>
        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                // 质量状态列着色
                if (dataGridView.Columns[e.ColumnIndex].Name == "qualityStatusColumn")
                {
                    if (e.Value != null)
                    {
                        string value = e.Value.ToString();
                        if (value == "PASS")
                        {
                            e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
                            e.CellStyle.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
                        }
                        else if (value == "UNPASS")
                        {
                            e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(245, 34, 45); // 红色
                            e.CellStyle.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.LogError($"单元格格式化异常: {ex.Message}");
            }
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
                    Status = (p.IsCompleted ?? false) ? "已完成" : "进行中",

                    // 质量状态
                    QualityStatus = (p.IsNG ?? false) ? "UNPASS" : "PASS",

                    // 保留NGProcess字段（用于后续可能的扩展，但不在UI显示）
                    NGProcess = (p.IsNG ?? false) && !string.IsNullOrEmpty(p.NGProcessId)
                        ? (p.NGProcessId == "14" ? "工序14" : "")
                        : "",

                    CreatedTime = p.CreatedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    CompletedTime = p.CompletedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    IsUploaded = (p.IsUploaded ?? false) ? "已上传" : "未上传",
                    UploadedTime = p.UploadedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                    OriginalData = p
                }).ToList();

                RefreshDisplayData(allData);

                LogManager.LogInfo($"加载了 {productDataList.Count} 条生产数据");
                UpdateStatusInfo();
                UpdateStatistics();
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

        /// <summary>
        /// 简化后的筛选逻辑 - 移除NG工序筛选
        /// </summary>
        private void ApplyCurrentFilter()
        {
            try
            {
                var searchText = searchTextBox.Text?.Trim().ToLower() ?? "";
                var selectedUploadStatus = uploadStatusComboBox.SelectedValue?.ToString() ?? "全部数据";
                var selectedQualityStatus = qualityStatusComboBox.SelectedValue?.ToString() ?? "全部数据";

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

                    // 质量状态筛选
                    bool matchesQualityStatus = selectedQualityStatus == "全部数据" ||
                        (selectedQualityStatus == "PASS产品" && item.QualityStatus == "PASS") ||
                        (selectedQualityStatus == "UNPASS产品" && item.QualityStatus == "UNPASS");

                    return matchesSearch && matchesUploadStatus && matchesQualityStatus;
                }).ToList();

                RefreshDisplayData(filteredData);
                UpdateStatistics();

                LogManager.LogInfo($"筛选完成，显示 {filteredData.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"筛选失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 简化后的统计信息 - 移除工序14统计
        /// </summary>
        private void UpdateStatistics()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(UpdateStatistics));
                    return;
                }

                var totalCount = displayData.Count;
                var okCount = displayData.Count(d => d.QualityStatus == "PASS");
                var ngCount = displayData.Count(d => d.QualityStatus == "UNPASS");

                // 计算合格率
                var qualityRate = totalCount > 0 ? (okCount * 100.0 / totalCount) : 0;

                var statsText = $"总数：{totalCount}  |  " +
                               $"PASS：{okCount} ({qualityRate:F1}%)  |  " +
                               $"UNPASS：{ngCount}";

                statsLabel.Text = statsText;

                // 根据数据状态设置颜色
                if (ngCount == 0)
                {
                    statsLabel.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
                }
                else if (qualityRate >= 95)
                {
                    statsLabel.ForeColor = System.Drawing.Color.FromArgb(250, 173, 20); // 橙色
                }
                else
                {
                    statsLabel.ForeColor = System.Drawing.Color.FromArgb(245, 34, 45); // 红色
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError($"更新统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 简化后的质量状态筛选事件 - 移除NG工序显示/隐藏逻辑
        /// </summary>
        private void qualityStatusComboBox_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            try
            {
                ApplyCurrentFilter();
            }
            catch (Exception ex)
            {
                LogManager.LogError($"质量状态筛选失败: {ex.Message}");
            }
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyCurrentFilter();
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

        #region 批量上传方法

        /// <summary>
        /// 批量上传按钮点击事件
        /// </summary>
        private async void batchUploadButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. 筛选未上传的记录
                var failedItems = allData.Where(item => item.IsUploaded == "未上传").ToList();

                if (failedItems.Count == 0)
                {
                    ShowMessage("没有需要上传的数据", MessageType.Info);
                    return;
                }

                // 2. 确认对话框
                var result = MessageBox.Show(
                    $"共有 {failedItems.Count} 条未上传的数据，确定要全部重新上传吗？",
                    "批量上传确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                // 3. 开始批量上传
                await BatchUploadData(failedItems);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"批量上传异常: {ex.Message}");
                ShowMessage($"批量上传异常: {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// 批量上传数据（串行上传）
        /// </summary>
        private async Task BatchUploadData(List<ProductDataViewModel> items)
        {
            if (isBatchUploading)
            {
                ShowMessage("批量上传正在进行中，请稍候...", MessageType.Warning);
                return;
            }

            isBatchUploading = true;

            try
            {
                // 禁用按钮
                SetBatchUploadingState(true);

                int successCount = 0;
                int failedCount = 0;
                var failedBarcodes = new List<string>();

                LogManager.LogInfo($"开始批量上传，共 {items.Count} 条记录");

                // 串行上传每条记录
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    try
                    {
                        // 更新进度提示
                        UpdateBatchUploadProgress(i + 1, items.Count, item.Barcode);

                        // 执行上传
                        using (var tempDataManager = new DataManager())
                        {
                            bool success = await tempDataManager.UploadToServer(
                                item.Barcode,
                                item.OriginalData.CompleteData);

                            if (success)
                            {
                                successCount++;
                                LogManager.LogInfo($"批量上传成功 [{i + 1}/{items.Count}]: {item.Barcode}");
                            }
                            else
                            {
                                failedCount++;
                                failedBarcodes.Add(item.Barcode);
                                LogManager.LogWarning($"批量上传失败 [{i + 1}/{items.Count}]: {item.Barcode}");
                            }
                        }

                        // 避免请求过快，间隔100ms
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        failedBarcodes.Add(item.Barcode);
                        LogManager.LogError($"批量上传异常 [{i + 1}/{items.Count}]: {item.Barcode}, 错误: {ex.Message}");
                    }
                }

                // 刷新数据
                LoadData();

                // 显示结果
                ShowBatchUploadResult(successCount, failedCount, failedBarcodes);
            }
            finally
            {
                isBatchUploading = false;
                SetBatchUploadingState(false);
            }
        }

        /// <summary>
        /// 设置批量上传状态（禁用/启用相关按钮）
        /// </summary>
        private void SetBatchUploadingState(bool uploading)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetBatchUploadingState), uploading);
                return;
            }

            // 禁用批量上传按钮
            batchUploadButton.Enabled = !uploading;
            batchUploadButton.Loading = uploading;
            batchUploadButton.Text = uploading ? "上传中..." : "批量上传";

            // 禁用刷新和导出按钮
            refreshButton.Enabled = !uploading;
            exportButton.Enabled = !uploading;

            // 禁用DataGridView（防止用户点击单条上传）
            dataGridView.Enabled = !uploading;
        }

        /// <summary>
        /// 更新批量上传进度
        /// </summary>
        private void UpdateBatchUploadProgress(int current, int total, string barcode)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, int, string>(UpdateBatchUploadProgress), current, total, barcode);
                return;
            }

            var progressText = $"上传中... ({current}/{total}) - {barcode}";
            batchUploadButton.Text = progressText;

            // 可选：更新统计标签显示进度
            statsLabel.Text = $"正在上传：{current}/{total}";
        }

        /// <summary>
        /// 显示批量上传结果
        /// </summary>
        private void ShowBatchUploadResult(int successCount, int failedCount, List<string> failedBarcodes)
        {
            var resultMessage = new StringBuilder();
            resultMessage.AppendLine($"批量上传完成！");
            resultMessage.AppendLine($"成功：{successCount} 条");
            resultMessage.AppendLine($"失败：{failedCount} 条");

            if (failedCount > 0 && failedBarcodes.Count > 0)
            {
                resultMessage.AppendLine();
                resultMessage.AppendLine("失败的条码：");

                // 最多显示10条
                var displayCount = Math.Min(failedBarcodes.Count, 10);
                for (int i = 0; i < displayCount; i++)
                {
                    resultMessage.AppendLine($"  - {failedBarcodes[i]}");
                }

                if (failedBarcodes.Count > 10)
                {
                    resultMessage.AppendLine($"  ... 还有 {failedBarcodes.Count - 10} 条");
                }
            }

            LogManager.LogInfo($"批量上传完成 - 成功:{successCount}, 失败:{failedCount}");

            // 根据结果显示不同类型的消息
            if (failedCount == 0)
            {
                ShowMessage($"批量上传成功！共 {successCount} 条", MessageType.Success);
            }
            else if (successCount == 0)
            {
                MessageBox.Show(resultMessage.ToString(), "批量上传失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(resultMessage.ToString(), "批量上传部分成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            // 只保留Excel格式
                            Filter = "Excel文件 (*.xlsx)|*.xlsx",
                            FileName = $"生产数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                        };

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            ExportToExcel(saveFileDialog.FileName);
                            result = true;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogManager.LogError($"导出操作失败: {ex.Message}");
                    throw;
                }
                return result;
            });
        }


        private void ExportToCSV(string fileName)
        {
            try
            {
                var csv = new StringBuilder();
                csv.Append('\uFEFF');

                csv.AppendLine("产品条码,流程状态,质量状态,创建时间,完成时间,上传状态,上传时间");

                foreach (var item in displayData)
                {
                    csv.AppendLine($"{EscapeCsvField(item.Barcode)}," +
                                  $"{EscapeCsvField(item.Status)}," +
                                  $"{EscapeCsvField(item.QualityStatus)}," +
                                  $"{EscapeCsvField(item.CreatedTime)}," +
                                  $"{EscapeCsvField(item.CompletedTime)}," +
                                  $"{EscapeCsvField(item.IsUploaded)}," +
                                  $"{EscapeCsvField(item.UploadedTime)}");
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

        /// <summary>
        /// 导出为Excel文件（使用EPPlus）
        /// </summary>
        private void ExportToExcel(string fileName)
        {
            try
            {
                LogManager.LogInfo($"开始导出Excel: {fileName}");

                // 删除已存在的文件
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                using (var package = new ExcelPackage(new FileInfo(fileName)))
                {
                    // 创建工作表
                    var worksheet = package.Workbook.Worksheets.Add("生产数据");

                    // ===== 1. 设置表头 =====
                    worksheet.Cells[1, 1].Value = "产品条码";
                    worksheet.Cells[1, 2].Value = "质量状态";
                    worksheet.Cells[1, 3].Value = "扭矩";
                    worksheet.Cells[1, 4].Value = "完成时间";

                    // 表头样式
                    using (var headerRange = worksheet.Cells[1, 1, 1, 4])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.Size = 12;
                        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    // ===== 2. 填充数据 =====
                    int row = 2;
                    foreach (var item in displayData)
                    {
                        // 列1：条码
                        worksheet.Cells[row, 1].Value = item.Barcode ?? "N/A";

                        // 列2：质量状态
                        worksheet.Cells[row, 2].Value = item.QualityStatus ?? "N/A";

                        // 列3：扭矩（从数据库中提取）
                        string torqueValue = ExtractTorqueFromProductData(item.OriginalData);
                        worksheet.Cells[row, 3].Value = torqueValue;

                        // 列4：完成时间
                        worksheet.Cells[row, 4].Value = item.CompletedTime ?? "N/A";

                        // 质量状态单元格颜色
                        if (item.QualityStatus == "PASS")
                        {
                            worksheet.Cells[row, 2].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(82, 196, 26)); // 绿色
                            worksheet.Cells[row, 2].Style.Font.Bold = true;
                        }
                        else if (item.QualityStatus == "UNPASS")
                        {
                            worksheet.Cells[row, 2].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(245, 34, 45)); // 红色
                            worksheet.Cells[row, 2].Style.Font.Bold = true;
                        }

                        // 添加边框
                        using (var rowRange = worksheet.Cells[row, 1, row, 4])
                        {
                            rowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            rowRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }

                        row++;
                    }

                    // ===== 3. 设置列宽 =====
                    worksheet.Column(1).Width = 20;  // 产品条码
                    worksheet.Column(2).Width = 15;  // 质量状态
                    worksheet.Column(3).Width = 15;  // 扭矩
                    worksheet.Column(4).Width = 25;  // 完成时间

                    // ===== 4. 设置所有数据单元格居中对齐 =====
                    var dataRange = worksheet.Cells[2, 1, row - 1, 4];
                    dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    // ===== 5. 自动筛选 =====
                    worksheet.Cells[1, 1, row - 1, 4].AutoFilter = true;

                    // ===== 6. 冻结首行 =====
                    worksheet.View.FreezePanes(2, 1);

                    // ===== 7. 保存文件 =====
                    package.Save();
                }

                LogManager.LogInfo($"Excel导出成功: {fileName}, 共 {displayData.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Excel导出失败: {ex.Message}");
                LogManager.LogError($"异常堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 从ProductData中提取扭矩值
        /// </summary>
        private string ExtractTorqueFromProductData(ProductData productData)
        {
            try
            {
                if (productData == null)
                {
                    return "N/A";
                }

                // 方法1：优先从 Process4_Data 中提取（工序14数据）
                if (!string.IsNullOrEmpty(productData.Process4_Data))
                {
                    var torque = ParseTorqueFromJson(productData.Process4_Data);
                    if (!string.IsNullOrEmpty(torque) && torque != "N/A")
                    {
                        return torque;
                    }
                }

                // 方法2：从 CompleteData 中提取（备用）
                if (!string.IsNullOrEmpty(productData.CompleteData))
                {
                    var torque = ParseTorqueFromJson(productData.CompleteData);
                    if (!string.IsNullOrEmpty(torque) && torque != "N/A")
                    {
                        return torque;
                    }
                }

                return "N/A";
            }
            catch (Exception ex)
            {
                LogManager.LogError($"提取扭矩值失败: {ex.Message}, 条码: {productData?.Barcode}");
                return "解析失败";
            }
        }

        /// <summary>
        /// 从JSON字符串中解析扭矩值
        /// </summary>
        private string ParseTorqueFromJson(string jsonData)
        {
            try
            {
                // 尝试解析JSON
                var jToken = Newtonsoft.Json.Linq.JToken.Parse(jsonData);

                // 如果是数组，取第一个元素
                if (jToken is Newtonsoft.Json.Linq.JArray jArray && jArray.Count > 0)
                {
                    jToken = jArray[0];
                }

                // 获取 Data 数组
                var dataArray = jToken["Data"];
                if (dataArray != null && dataArray.HasValues)
                {
                    var remark = dataArray[0]["Remark"]?.ToString();
                    if (!string.IsNullOrEmpty(remark))
                    {
                        // 从 "扭矩：5.23Nm" 中提取 "5.23Nm"
                        if (remark.Contains("扭矩："))
                        {
                            var torque = remark.Replace("扭矩：", "").Trim();
                            return torque;
                        }
                    }
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
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
        public string QualityStatus { get; set; }
        public string NGProcess { get; set; }
        public string CreatedTime { get; set; }
        public string CompletedTime { get; set; }
        public string IsUploaded { get; set; }
        public string UploadedTime { get; set; }
        public ProductData OriginalData { get; set; }
    }
}

