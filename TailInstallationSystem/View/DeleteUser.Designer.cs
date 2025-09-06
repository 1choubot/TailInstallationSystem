namespace TailInstallationSystem.View
{
    partial class DeleteUser
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
            this.panel1 = new AntdUI.Panel();
            this.dgvUsers = new System.Windows.Forms.DataGridView();
            this.UserName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CreatedTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDelete = new System.Windows.Forms.DataGridViewButtonColumn();
            this.btnSearch = new AntdUI.Button();
            this.txtSearch = new AntdUI.Input();
            this.label2 = new AntdUI.Label();
            this.label1 = new AntdUI.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dgvUsers);
            this.panel1.Controls.Add(this.btnSearch);
            this.panel1.Controls.Add(this.txtSearch);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(192, 109);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(904, 595);
            this.panel1.TabIndex = 0;
            this.panel1.Text = "panel1";
            // 
            // dgvUsers
            // 
            this.dgvUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvUsers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.UserName,
            this.CreatedTime,
            this.colDelete});
            this.dgvUsers.Location = new System.Drawing.Point(183, 214);
            this.dgvUsers.Name = "dgvUsers";
            this.dgvUsers.RowHeadersWidth = 51;
            this.dgvUsers.RowTemplate.Height = 27;
            this.dgvUsers.Size = new System.Drawing.Size(538, 331);
            this.dgvUsers.TabIndex = 4;
            // 
            // UserName
            // 
            this.UserName.HeaderText = "用户名";
            this.UserName.MinimumWidth = 6;
            this.UserName.Name = "UserName";
            this.UserName.Width = 125;
            // 
            // CreatedTime
            // 
            this.CreatedTime.HeaderText = "创建时间";
            this.CreatedTime.MinimumWidth = 6;
            this.CreatedTime.Name = "CreatedTime";
            this.CreatedTime.Width = 125;
            // 
            // colDelete
            // 
            this.colDelete.HeaderText = "操作";
            this.colDelete.MinimumWidth = 6;
            this.colDelete.Name = "colDelete";
            this.colDelete.Text = "删除";
            this.colDelete.Width = 125;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(607, 133);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(85, 42);
            this.btnSearch.TabIndex = 3;
            this.btnSearch.Text = "搜索";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(348, 133);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(218, 42);
            this.txtSearch.TabIndex = 2;
            this.txtSearch.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(208, 133);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 42);
            this.label2.TabIndex = 0;
            this.label2.Text = "用户名";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(396, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 52);
            this.label1.TabIndex = 0;
            this.label1.Text = "删除用户";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DeleteUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Name = "DeleteUser";
            this.Size = new System.Drawing.Size(1290, 788);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Panel panel1;
        private AntdUI.Button btnSearch;
        private AntdUI.Input txtSearch;
        private AntdUI.Label label2;
        private AntdUI.Label label1;
        private System.Windows.Forms.DataGridView dgvUsers;
        private System.Windows.Forms.DataGridViewTextBoxColumn UserName;
        private System.Windows.Forms.DataGridViewTextBoxColumn CreatedTime;
        private System.Windows.Forms.DataGridViewButtonColumn colDelete;
    }
}
