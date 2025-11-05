namespace TailInstallationSystem.Forms
{
    partial class ProductDetailForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainPanel = new AntdUI.Panel();
            this.contentPanel = new AntdUI.Panel();
            this.jsonCard = new AntdUI.Panel();
            this.jsonTextBox = new System.Windows.Forms.TextBox();
            this.jsonTitleLabel = new AntdUI.Label();
            this.infoCard = new AntdUI.Panel();
            this.infoTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblQualityStatus = new AntdUI.Label();
            this.lblQualityStatusValue = new AntdUI.Label();
            this.lblUploadTime = new AntdUI.Label();
            this.lblUploadTimeValue = new AntdUI.Label();
            this.lblUploadStatus = new AntdUI.Label();
            this.lblUploadStatusValue = new AntdUI.Label();
            this.lblCompletedTime = new AntdUI.Label();
            this.lblCompletedTimeValue = new AntdUI.Label();
            this.lblCreatedTime = new AntdUI.Label();
            this.lblCreatedTimeValue = new AntdUI.Label();
            this.lblStatus = new AntdUI.Label();
            this.lblStatusValue = new AntdUI.Label();
            this.lblBarcode = new AntdUI.Label();
            this.lblBarcodeValue = new AntdUI.Label();
            this.infoTitleLabel = new AntdUI.Label();
            this.buttonPanel = new AntdUI.Panel();
            this.btnClose = new AntdUI.Button();
            this.btnCopyJson = new AntdUI.Button();
            this.headerPanel = new AntdUI.Panel();
            this.titleLabel = new AntdUI.Label();
            this.mainPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.jsonCard.SuspendLayout();
            this.infoCard.SuspendLayout();
            this.infoTableLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.contentPanel);
            this.mainPanel.Controls.Add(this.buttonPanel);
            this.mainPanel.Controls.Add(this.headerPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(16);
            this.mainPanel.Size = new System.Drawing.Size(900, 700);
            this.mainPanel.TabIndex = 0;
            // 
            // contentPanel
            // 
            this.contentPanel.Controls.Add(this.jsonCard);
            this.contentPanel.Controls.Add(this.infoCard);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(16, 80);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(868, 556);
            this.contentPanel.TabIndex = 2;
            // 
            // jsonCard
            // 
            this.jsonCard.BorderWidth = 1;
            this.jsonCard.Controls.Add(this.jsonTextBox);
            this.jsonCard.Controls.Add(this.jsonTitleLabel);
            this.jsonCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jsonCard.Location = new System.Drawing.Point(0, 250);
            this.jsonCard.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.jsonCard.Name = "jsonCard";
            this.jsonCard.Padding = new System.Windows.Forms.Padding(16);
            this.jsonCard.Radius = 8;
            this.jsonCard.Shadow = 2;
            this.jsonCard.Size = new System.Drawing.Size(868, 306);
            this.jsonCard.TabIndex = 1;
            // 
            // jsonTextBox
            // 
            this.jsonTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jsonTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.jsonTextBox.Location = new System.Drawing.Point(16, 52);
            this.jsonTextBox.Multiline = true;
            this.jsonTextBox.Name = "jsonTextBox";
            this.jsonTextBox.ReadOnly = true;
            this.jsonTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.jsonTextBox.Size = new System.Drawing.Size(836, 238);
            this.jsonTextBox.TabIndex = 1;
            this.jsonTextBox.WordWrap = true;
            // 
            // jsonTitleLabel
            // 
            this.jsonTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.jsonTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.jsonTitleLabel.Location = new System.Drawing.Point(16, 16);
            this.jsonTitleLabel.Name = "jsonTitleLabel";
            this.jsonTitleLabel.Size = new System.Drawing.Size(836, 36);
            this.jsonTitleLabel.TabIndex = 0;
            this.jsonTitleLabel.Text = "详细数据 (JSON格式)";
            this.jsonTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // infoCard
            // 
            this.infoCard.BorderWidth = 1;
            this.infoCard.Controls.Add(this.infoTableLayout);
            this.infoCard.Controls.Add(this.infoTitleLabel);
            this.infoCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.infoCard.Location = new System.Drawing.Point(0, 0);
            this.infoCard.Name = "infoCard";
            this.infoCard.Padding = new System.Windows.Forms.Padding(16);
            this.infoCard.Radius = 8;
            this.infoCard.Shadow = 2;
            this.infoCard.Size = new System.Drawing.Size(868, 250);
            this.infoCard.TabIndex = 0;
            // 
            // infoTableLayout
            // 
            this.infoTableLayout.ColumnCount = 4;
            this.infoTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.infoTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.infoTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.infoTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.infoTableLayout.Controls.Add(this.lblQualityStatus, 0, 3);
            this.infoTableLayout.Controls.Add(this.lblQualityStatusValue, 1, 3);
            this.infoTableLayout.Controls.Add(this.lblUploadTime, 2, 2);
            this.infoTableLayout.Controls.Add(this.lblUploadTimeValue, 3, 2);
            this.infoTableLayout.Controls.Add(this.lblUploadStatus, 0, 2);
            this.infoTableLayout.Controls.Add(this.lblUploadStatusValue, 1, 2);
            this.infoTableLayout.Controls.Add(this.lblCompletedTime, 2, 1);
            this.infoTableLayout.Controls.Add(this.lblCompletedTimeValue, 3, 1);
            this.infoTableLayout.Controls.Add(this.lblCreatedTime, 0, 1);
            this.infoTableLayout.Controls.Add(this.lblCreatedTimeValue, 1, 1);
            this.infoTableLayout.Controls.Add(this.lblStatus, 2, 0);
            this.infoTableLayout.Controls.Add(this.lblStatusValue, 3, 0);
            this.infoTableLayout.Controls.Add(this.lblBarcode, 0, 0);
            this.infoTableLayout.Controls.Add(this.lblBarcodeValue, 1, 0);
            this.infoTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoTableLayout.Location = new System.Drawing.Point(16, 52);
            this.infoTableLayout.Name = "infoTableLayout";
            this.infoTableLayout.RowCount = 4;
            this.infoTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoTableLayout.Size = new System.Drawing.Size(836, 182);
            this.infoTableLayout.TabIndex = 1;
            // 
            // lblQualityStatus
            // 
            this.lblQualityStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblQualityStatus.AutoSize = true;
            this.lblQualityStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblQualityStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblQualityStatus.Location = new System.Drawing.Point(3, 150);
            this.lblQualityStatus.Name = "lblQualityStatus";
            this.lblQualityStatus.Size = new System.Drawing.Size(68, 17);
            this.lblQualityStatus.TabIndex = 13;
            this.lblQualityStatus.Text = "质量状态：";
            // 
            // lblQualityStatusValue
            // 
            this.lblQualityStatusValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblQualityStatusValue.AutoSize = true;
            this.lblQualityStatusValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblQualityStatusValue.Location = new System.Drawing.Point(128, 150);
            this.lblQualityStatusValue.Name = "lblQualityStatusValue";
            this.lblQualityStatusValue.Size = new System.Drawing.Size(30, 17);
            this.lblQualityStatusValue.TabIndex = 12;
            this.lblQualityStatusValue.Text = "N/A";
            // 
            // lblUploadTime
            // 
            this.lblUploadTime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblUploadTime.AutoSize = true;
            this.lblUploadTime.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUploadTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblUploadTime.Location = new System.Drawing.Point(420, 105);
            this.lblUploadTime.Name = "lblUploadTime";
            this.lblUploadTime.Size = new System.Drawing.Size(68, 17);
            this.lblUploadTime.TabIndex = 11;
            this.lblUploadTime.Text = "上传时间：";
            // 
            // lblUploadTimeValue
            // 
            this.lblUploadTimeValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblUploadTimeValue.AutoSize = true;
            this.lblUploadTimeValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblUploadTimeValue.Location = new System.Drawing.Point(545, 105);
            this.lblUploadTimeValue.Name = "lblUploadTimeValue";
            this.lblUploadTimeValue.Size = new System.Drawing.Size(30, 17);
            this.lblUploadTimeValue.TabIndex = 10;
            this.lblUploadTimeValue.Text = "N/A";
            // 
            // lblUploadStatus
            // 
            this.lblUploadStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblUploadStatus.AutoSize = true;
            this.lblUploadStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUploadStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblUploadStatus.Location = new System.Drawing.Point(3, 105);
            this.lblUploadStatus.Name = "lblUploadStatus";
            this.lblUploadStatus.Size = new System.Drawing.Size(68, 17);
            this.lblUploadStatus.TabIndex = 9;
            this.lblUploadStatus.Text = "上传状态：";
            // 
            // lblUploadStatusValue
            // 
            this.lblUploadStatusValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblUploadStatusValue.AutoSize = true;
            this.lblUploadStatusValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblUploadStatusValue.Location = new System.Drawing.Point(128, 105);
            this.lblUploadStatusValue.Name = "lblUploadStatusValue";
            this.lblUploadStatusValue.Size = new System.Drawing.Size(30, 17);
            this.lblUploadStatusValue.TabIndex = 8;
            this.lblUploadStatusValue.Text = "N/A";
            // 
            // lblCompletedTime
            // 
            this.lblCompletedTime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCompletedTime.AutoSize = true;
            this.lblCompletedTime.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCompletedTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompletedTime.Location = new System.Drawing.Point(420, 59);
            this.lblCompletedTime.Name = "lblCompletedTime";
            this.lblCompletedTime.Size = new System.Drawing.Size(68, 17);
            this.lblCompletedTime.TabIndex = 7;
            this.lblCompletedTime.Text = "完成时间：";
            // 
            // lblCompletedTimeValue
            // 
            this.lblCompletedTimeValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCompletedTimeValue.AutoSize = true;
            this.lblCompletedTimeValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblCompletedTimeValue.Location = new System.Drawing.Point(545, 59);
            this.lblCompletedTimeValue.Name = "lblCompletedTimeValue";
            this.lblCompletedTimeValue.Size = new System.Drawing.Size(30, 17);
            this.lblCompletedTimeValue.TabIndex = 6;
            this.lblCompletedTimeValue.Text = "N/A";
            // 
            // lblCreatedTime
            // 
            this.lblCreatedTime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCreatedTime.AutoSize = true;
            this.lblCreatedTime.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCreatedTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCreatedTime.Location = new System.Drawing.Point(3, 59);
            this.lblCreatedTime.Name = "lblCreatedTime";
            this.lblCreatedTime.Size = new System.Drawing.Size(68, 17);
            this.lblCreatedTime.TabIndex = 5;
            this.lblCreatedTime.Text = "创建时间：";
            // 
            // lblCreatedTimeValue
            // 
            this.lblCreatedTimeValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCreatedTimeValue.AutoSize = true;
            this.lblCreatedTimeValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblCreatedTimeValue.Location = new System.Drawing.Point(128, 59);
            this.lblCreatedTimeValue.Name = "lblCreatedTimeValue";
            this.lblCreatedTimeValue.Size = new System.Drawing.Size(30, 17);
            this.lblCreatedTimeValue.TabIndex = 4;
            this.lblCreatedTimeValue.Text = "N/A";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblStatus.Location = new System.Drawing.Point(420, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(44, 17);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "状态：";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblStatusValue.Location = new System.Drawing.Point(545, 14);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(30, 17);
            this.lblStatusValue.TabIndex = 2;
            this.lblStatusValue.Text = "N/A";
            // 
            // lblBarcode
            // 
            this.lblBarcode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBarcode.AutoSize = true;
            this.lblBarcode.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBarcode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblBarcode.Location = new System.Drawing.Point(3, 14);
            this.lblBarcode.Name = "lblBarcode";
            this.lblBarcode.Size = new System.Drawing.Size(68, 17);
            this.lblBarcode.TabIndex = 1;
            this.lblBarcode.Text = "产品条码：";
            // 
            // lblBarcodeValue
            // 
            this.lblBarcodeValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBarcodeValue.AutoSize = true;
            this.lblBarcodeValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblBarcodeValue.Location = new System.Drawing.Point(128, 14);
            this.lblBarcodeValue.Name = "lblBarcodeValue";
            this.lblBarcodeValue.Size = new System.Drawing.Size(30, 17);
            this.lblBarcodeValue.TabIndex = 0;
            this.lblBarcodeValue.Text = "N/A";
            // 
            // infoTitleLabel
            // 
            this.infoTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.infoTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.infoTitleLabel.Location = new System.Drawing.Point(16, 16);
            this.infoTitleLabel.Name = "infoTitleLabel";
            this.infoTitleLabel.Size = new System.Drawing.Size(836, 36);
            this.infoTitleLabel.TabIndex = 0;
            this.infoTitleLabel.Text = "基本信息";
            this.infoTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Controls.Add(this.btnCopyJson);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(16, 636);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(868, 48);
            this.buttonPanel.TabIndex = 1;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(788, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 32);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "关闭";
            this.btnClose.Type = AntdUI.TTypeMini.Default;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnCopyJson
            // 
            this.btnCopyJson.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyJson.Location = new System.Drawing.Point(688, 8);
            this.btnCopyJson.Name = "btnCopyJson";
            this.btnCopyJson.Size = new System.Drawing.Size(94, 32);
            this.btnCopyJson.TabIndex = 0;
            this.btnCopyJson.Text = "复制JSON";
            this.btnCopyJson.Type = AntdUI.TTypeMini.Primary;
            this.btnCopyJson.Click += new System.EventHandler(this.btnCopyJson_Click);
            // 
            // headerPanel
            // 
            this.headerPanel.BorderWidth = 0;
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(16, 16);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(868, 64);
            this.headerPanel.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(868, 64);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "产品详情";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ProductDetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Controls.Add(this.mainPanel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(700, 500);
            this.Name = "ProductDetailForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "产品详情";
            this.mainPanel.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.jsonCard.ResumeLayout(false);
            this.infoCard.ResumeLayout(false);
            this.infoTableLayout.ResumeLayout(false);
            this.infoTableLayout.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Panel mainPanel;
        private AntdUI.Panel headerPanel;
        private AntdUI.Label titleLabel;
        private AntdUI.Panel buttonPanel;
        private AntdUI.Button btnClose;
        private AntdUI.Button btnCopyJson;
        private AntdUI.Panel contentPanel;
        private AntdUI.Panel infoCard;
        private AntdUI.Label infoTitleLabel;
        private System.Windows.Forms.TableLayoutPanel infoTableLayout;
        private AntdUI.Label lblBarcode;
        private AntdUI.Label lblBarcodeValue;
        private AntdUI.Label lblStatus;
        private AntdUI.Label lblStatusValue;
        private AntdUI.Label lblCreatedTime;
        private AntdUI.Label lblCreatedTimeValue;
        private AntdUI.Label lblCompletedTime;
        private AntdUI.Label lblCompletedTimeValue;
        private AntdUI.Label lblUploadStatus;
        private AntdUI.Label lblUploadStatusValue;
        private AntdUI.Label lblUploadTime;
        private AntdUI.Label lblUploadTimeValue;
        private AntdUI.Label lblQualityStatus;
        private AntdUI.Label lblQualityStatusValue;
        private AntdUI.Panel jsonCard;
        private AntdUI.Label jsonTitleLabel;
        private System.Windows.Forms.TextBox jsonTextBox;

        private AntdUI.Label lblNGProcess = null;
        private AntdUI.Label lblNGProcessValue = null;
    }
}

