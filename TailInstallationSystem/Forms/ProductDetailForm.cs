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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

                // 显示质量状态
                lblQualityStatusValue.Text = _productData.QualityStatus ?? "N/A";

                // 不再显示NG工序信息
                // lblNGProcessValue.Text = _productData.NGProcess ?? "无";

                // 设置状态颜色
                SetStatusColor();

                // 隐藏NG工序显示控件
                HideNGProcessInfo();

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

                // 如果是UNPASS产品，解析并显示不合格原因
                if (_productData.QualityStatus == "UNPASS")
                {
                    string ngReason = ExtractNGReasonFromJson(completeJsonData);

                    if (!string.IsNullOrEmpty(ngReason))
                    {
                        this.Text = $"产品详情 - {_productData.Barcode} [UNPASS: {ngReason}]";
                        titleLabel.Text = $"产品详情 - {_productData.Barcode} [UNPASS: {ngReason}]";
                    }
                    else
                    {
                        this.Text = $"产品详情 - {_productData.Barcode} [UNPASS产品]";
                        titleLabel.Text = $"产品详情 - {_productData.Barcode} [UNPASS产品]";
                    }

                    titleLabel.ForeColor = System.Drawing.Color.FromArgb(245, 34, 45);
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

            // 设置质量状态颜色
            if (_productData.QualityStatus == "PASS")
            {
                lblQualityStatusValue.ForeColor = System.Drawing.Color.FromArgb(82, 196, 26); // 绿色
                lblQualityStatusValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            }
            else if (_productData.QualityStatus == "UNPASS")
            {
                lblQualityStatusValue.ForeColor = System.Drawing.Color.FromArgb(245, 34, 45); // 红色
                lblQualityStatusValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
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

        /// <summary>
        /// 隐藏NG工序显示控件
        /// </summary>
        private void HideNGProcessInfo()
        {
            // 直接隐藏NG工序相关控件（不再根据是否NG判断）
            if (lblNGProcess != null)
                lblNGProcess.Visible = false;

            if (lblNGProcessValue != null)
                lblNGProcessValue.Visible = false;
        }

        /// <summary>
        /// 从JSON数据中提取UNPASS原因
        /// </summary>
        private string ExtractNGReasonFromJson(string jsonData)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    return null;

                JObject process14Data = null;

                // 尝试解析为 JToken（自动识别类型）
                var jToken = JToken.Parse(jsonData);

                if (jToken is JArray jArray)
                {
                    if (jArray.Count > 0)
                    {
                        process14Data = jArray[0] as JObject;
                    }
                }
                else if (jToken is JObject jObject)
                {
                    process14Data = jObject;
                }

                if (process14Data == null)
                {
                    LogManager.LogWarning("无法解析产品数据：格式异常");
                    return null;
                }

                // 检查是否有Data数组
                var dataItems = process14Data["Data"] as JArray;
                if (dataItems == null || dataItems.Count == 0)
                    return null;

                // 获取第一个Data项
                var firstDataItem = dataItems[0];

                // 获取Result字段
                string result = firstDataItem["Result"]?.ToString();

                if (result == "UNPASS")
                {
                    // 尝试从Remark中提取不合格原因
                    string remark = firstDataItem["Remark"]?.ToString();

                    if (!string.IsNullOrEmpty(remark))
                    {
                        // 解析Remark字段，提取具体原因
                        return ParseNGReason(remark);
                    }

                    // 如果没有Remark，尝试从ItemName判断
                    string itemName = firstDataItem["ItemName"]?.ToString();
                    if (itemName == "尾椎安装")
                    {
                        return "拧紧不合格";
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogManager.LogWarning($"解析UNPASS原因失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析Remark字段中的NG原因
        /// </summary>
        private string ParseNGReason(string remark)
        {
            try
            {
                // 示例Remark格式：
                // "扭矩：29.10Nm" - 提取扭矩值
                // "数据异常：拧紧数据读取失败" - 提取异常原因

                if (remark.Contains("扭矩过低") || remark.Contains("小于下限"))
                {
                    return "扭矩过低";
                }
                else if (remark.Contains("扭矩过高") || remark.Contains("大于上限"))
                {
                    return "扭矩过高";
                }
                else if (remark.Contains("超时") || remark.Contains("超最上限时间"))
                {
                    return "拧紧超时";
                }
                else if (remark.Contains("角度过低") || remark.Contains("小于下限角度"))
                {
                    return "角度过低";
                }
                else if (remark.Contains("角度过高") || remark.Contains("大于上限角度"))
                {
                    return "角度过高";
                }
                else if (remark.Contains("数据异常"))
                {
                    return "数据异常";
                }
                else if (remark.Contains("扭矩"))
                {
                    // 通用拧紧不合格
                    return "拧紧不合格";
                }

                // 如果无法识别具体原因，返回备注的前20个字符
                return remark.Length > 20 ? remark.Substring(0, 20) + "..." : remark;
            }
            catch
            {
                return "拧紧不合格";
            }
        }

        private string GetCompleteJsonData(ProductData originalData)
        {
            if (originalData == null)
                return null;

            if (!string.IsNullOrEmpty(originalData.CompleteData))
                return originalData.CompleteData;

            // 后备方案：从 Process4_Data 构造（理论上不应该进入这里）
            if (!string.IsNullOrEmpty(originalData.Process4_Data))
            {
                LogManager.LogWarning($"CompleteData为空，使用Process4_Data构造 - 条码:{originalData.Barcode}");
                return $"[{originalData.Process4_Data}]";  // 包装成数组格式
            }

            return null;
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
