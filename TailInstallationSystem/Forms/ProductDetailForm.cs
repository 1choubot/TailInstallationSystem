using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace TailInstallationSystem.Forms
{
    public partial class ProductDetailForm : AntdUI.Window
    {
        private ProductDataViewModel _productData;

        public ProductDetailForm()
        {
            InitializeComponent();
        }

        public ProductDetailForm(ProductDataViewModel productData) : this()
        {
            _productData = productData;
            LoadProductDetails();
        }

        private void LoadProductDetails()
        {
            try
            {
                if (_productData == null) return;

                // 设置窗口标题
                this.Text = $"产品详情 - {_productData.Barcode}";
                titleLabel.Text = $"产品详情 - {_productData.Barcode}";

                // 填充基本信息
                lblBarcodeValue.Text = _productData.Barcode ?? "N/A";
                lblStatusValue.Text = _productData.Status ?? "N/A";
                lblCreatedTimeValue.Text = _productData.CreatedTime ?? "N/A";
                lblCompletedTimeValue.Text = _productData.CompletedTime ?? "N/A";
                lblUploadStatusValue.Text = _productData.IsUploaded ?? "N/A";
                lblUploadTimeValue.Text = _productData.UploadedTime ?? "N/A";

                // 设置状态颜色
                SetStatusColor();

                // 格式化并显示完整的JSON数据
                var completeJsonData = GetCompleteJsonData(_productData.OriginalData);

                if (!string.IsNullOrEmpty(completeJsonData))
                {
                    try
                    {
                        // 尝试格式化JSON
                        var formattedJson = JsonConvert.SerializeObject(
                            JsonConvert.DeserializeObject(completeJsonData),
                            Formatting.Indented);
                        jsonTextBox.Text = formattedJson; 
                    }
                    catch
                    {
                        // 如果JSON格式化失败，直接显示原始数据
                        jsonTextBox.Text = completeJsonData; 
                    }
                }
                else
                {
                    jsonTextBox.Text = "暂无详细数据"; 
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"加载产品详情失败: {ex.Message}");
            }
        }

        private void SetStatusColor()
        {
            // 设置状态标签颜色
            if (_productData.Status == "已完成")
            {
                lblStatusValue.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
            }
            else
            {
                lblStatusValue.ForeColor = System.Drawing.Color.FromArgb(250, 173, 20); // 橙色
            }

            // 设置上传状态颜色
            if (_productData.IsUploaded == "已上传")
            {
                lblUploadStatusValue.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
            }
            else
            {
                lblUploadStatusValue.ForeColor = System.Drawing.Color.FromArgb(245, 34, 45); // 红色
            }
        }

        private string GetCompleteJsonData(ProductData originalData)
        {
            if (originalData == null)
                return null;

            // 优先返回 CompleteData，如果没有则组合其他数据
            if (!string.IsNullOrEmpty(originalData.CompleteData))
                return originalData.CompleteData;

            // 如果没有完整数据，组合各工序数据
            var combinedData = new
            {
                Barcode = originalData.Barcode,
                CreatedTime = originalData.CreatedTime,
                CompletedTime = originalData.CompletedTime,
                IsCompleted = originalData.IsCompleted,
                IsUploaded = originalData.IsUploaded,
                UploadedTime = originalData.UploadedTime,
                ProcessData = new
                {
                    Process1 = TryParseJson(originalData.Process1_Data),
                    Process2 = TryParseJson(originalData.Process2_Data),
                    Process3 = TryParseJson(originalData.Process3_Data),
                    Process4 = TryParseJson(originalData.Process4_Data)
                }
            };

            return JsonConvert.SerializeObject(combinedData, Formatting.Indented);
        }

        private object TryParseJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return null;

            try
            {
                return JsonConvert.DeserializeObject(jsonString);
            }
            catch
            {
                return jsonString; // 如果不是有效的JSON，返回原始字符串
            }
        }

        private void btnCopyJson_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonTextBox.Text)) 
                {
                    Clipboard.SetText(jsonTextBox.Text); 
                    AntdUI.Message.success(this, "JSON数据已复制到剪贴板", autoClose: 2);
                }
                else
                {
                    AntdUI.Message.warn(this, "没有可复制的数据", autoClose: 2);
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, $"复制失败: {ex.Message}");
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}