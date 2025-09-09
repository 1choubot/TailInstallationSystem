namespace TailInstallationSystem
{
    partial class DataViewControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.titleLabel = new AntdUI.Label();
            this.toolbarPanel = new AntdUI.Panel();
            this.searchTextBox = new AntdUI.Input();
            this.exportButton = new AntdUI.Button();
            this.refreshButton = new AntdUI.Button();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.barcodeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.createdTimeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.completedTimeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.isUploadedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.uploadedTimeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.actionsColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            this.toolbarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(27, 25);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(267, 50);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "生产数据查看";
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.toolbarPanel.Controls.Add(this.searchTextBox);
            this.toolbarPanel.Controls.Add(this.exportButton);
            this.toolbarPanel.Controls.Add(this.refreshButton);
            this.toolbarPanel.Location = new System.Drawing.Point(27, 88);
            this.toolbarPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Size = new System.Drawing.Size(1227, 62);
            this.toolbarPanel.TabIndex = 1;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.searchTextBox.Location = new System.Drawing.Point(933, 10);
            this.searchTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.PlaceholderText = "搜索条码...";
            this.searchTextBox.Size = new System.Drawing.Size(267, 44);
            this.searchTextBox.TabIndex = 2;
            this.searchTextBox.TextChanged += new System.EventHandler(this.searchTextBox_TextChanged);
            // 
            // exportButton
            // 
            this.exportButton.Location = new System.Drawing.Point(133, 10);
            this.exportButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(107, 44);
            this.exportButton.TabIndex = 1;
            this.exportButton.Text = "📤 导出";
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(13, 10);
            this.refreshButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(107, 44);
            this.refreshButton.TabIndex = 0;
            this.refreshButton.Text = "刷新";
            this.refreshButton.Type = AntdUI.TTypeMini.Primary;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.barcodeColumn,
            this.statusColumn,
            this.createdTimeColumn,
            this.completedTimeColumn,
            this.isUploadedColumn,
            this.uploadedTimeColumn,
            this.actionsColumn});
            this.dataGridView.Location = new System.Drawing.Point(27, 162);
            this.dataGridView.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowHeadersWidth = 51;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(1227, 625);
            this.dataGridView.TabIndex = 2;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            // 
            // barcodeColumn
            // 
            this.barcodeColumn.DataPropertyName = "Barcode";
            this.barcodeColumn.HeaderText = "产品条码";
            this.barcodeColumn.MinimumWidth = 6;
            this.barcodeColumn.Name = "barcodeColumn";
            this.barcodeColumn.ReadOnly = true;
            this.barcodeColumn.Width = 120;
            // 
            // statusColumn
            // 
            this.statusColumn.DataPropertyName = "Status";
            this.statusColumn.HeaderText = "状态";
            this.statusColumn.MinimumWidth = 6;
            this.statusColumn.Name = "statusColumn";
            this.statusColumn.ReadOnly = true;
            this.statusColumn.Width = 80;
            // 
            // createdTimeColumn
            // 
            this.createdTimeColumn.DataPropertyName = "CreatedTime";
            this.createdTimeColumn.HeaderText = "创建时间";
            this.createdTimeColumn.MinimumWidth = 6;
            this.createdTimeColumn.Name = "createdTimeColumn";
            this.createdTimeColumn.ReadOnly = true;
            this.createdTimeColumn.Width = 150;
            // 
            // completedTimeColumn
            // 
            this.completedTimeColumn.DataPropertyName = "CompletedTime";
            this.completedTimeColumn.HeaderText = "完成时间";
            this.completedTimeColumn.MinimumWidth = 6;
            this.completedTimeColumn.Name = "completedTimeColumn";
            this.completedTimeColumn.ReadOnly = true;
            this.completedTimeColumn.Width = 150;
            // 
            // isUploadedColumn
            // 
            this.isUploadedColumn.DataPropertyName = "IsUploaded";
            this.isUploadedColumn.HeaderText = "上传状态";
            this.isUploadedColumn.MinimumWidth = 6;
            this.isUploadedColumn.Name = "isUploadedColumn";
            this.isUploadedColumn.ReadOnly = true;
            // 
            // uploadedTimeColumn
            // 
            this.uploadedTimeColumn.DataPropertyName = "UploadedTime";
            this.uploadedTimeColumn.HeaderText = "上传时间";
            this.uploadedTimeColumn.MinimumWidth = 6;
            this.uploadedTimeColumn.Name = "uploadedTimeColumn";
            this.uploadedTimeColumn.ReadOnly = true;
            this.uploadedTimeColumn.Width = 150;
            // 
            // actionsColumn
            // 
            this.actionsColumn.HeaderText = "操作";
            this.actionsColumn.MinimumWidth = 6;
            this.actionsColumn.Name = "actionsColumn";
            this.actionsColumn.ReadOnly = true;
            this.actionsColumn.Text = "查看详情";
            this.actionsColumn.UseColumnTextForButtonValue = true;
            // 
            // DataViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.titleLabel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "DataViewControl";
            this.Padding = new System.Windows.Forms.Padding(27, 25, 27, 25);
            this.Size = new System.Drawing.Size(1280, 812);
            this.toolbarPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Label titleLabel;
        private AntdUI.Panel toolbarPanel;
        private AntdUI.Input searchTextBox;
        private AntdUI.Button exportButton;
        private AntdUI.Button refreshButton;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn barcodeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn statusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn createdTimeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn completedTimeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn isUploadedColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn uploadedTimeColumn;
        private System.Windows.Forms.DataGridViewButtonColumn actionsColumn;
    }
}
