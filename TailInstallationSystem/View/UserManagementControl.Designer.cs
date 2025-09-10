namespace TailInstallationSystem
{
    partial class UserManagementControl
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
            this.components = new System.ComponentModel.Container();
            this.userListView = new System.Windows.Forms.ListView();
            this.usernameHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.permissionHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.createdTimeHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lastLoginHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.statusHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.editUserMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteUserMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.resetPasswordMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topPanel = new System.Windows.Forms.Panel();
            this.addUserButton = new AntdUI.Button();
            this.titleLabel = new AntdUI.Label();
            this.contextMenuStrip.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // userListView
            // 
            this.userListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.userListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.usernameHeader,
            this.permissionHeader,
            this.createdTimeHeader,
            this.lastLoginHeader,
            this.statusHeader});
            this.userListView.ContextMenuStrip = this.contextMenuStrip;
            this.userListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userListView.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.userListView.FullRowSelect = true;
            this.userListView.GridLines = true;
            this.userListView.HideSelection = false;
            this.userListView.Location = new System.Drawing.Point(27, 106);
            this.userListView.Margin = new System.Windows.Forms.Padding(4);
            this.userListView.MultiSelect = false;
            this.userListView.Name = "userListView";
            this.userListView.Size = new System.Drawing.Size(1226, 619);
            this.userListView.TabIndex = 2;
            this.userListView.UseCompatibleStateImageBehavior = false;
            this.userListView.View = System.Windows.Forms.View.Details;
            this.userListView.DoubleClick += new System.EventHandler(this.userListView_DoubleClick);
            // 
            // usernameHeader
            // 
            this.usernameHeader.Text = "用户名";
            this.usernameHeader.Width = 150;
            // 
            // permissionHeader
            // 
            this.permissionHeader.Text = "权限级别";
            this.permissionHeader.Width = 100;
            // 
            // createdTimeHeader
            // 
            this.createdTimeHeader.Text = "创建时间";
            this.createdTimeHeader.Width = 150;
            // 
            // lastLoginHeader
            // 
            this.lastLoginHeader.Text = "最后登录";
            this.lastLoginHeader.Width = 150;
            // 
            // statusHeader
            // 
            this.statusHeader.Text = "状态";
            this.statusHeader.Width = 80;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editUserMenuItem,
            this.deleteUserMenuItem,
            this.toolStripSeparator1,
            this.resetPasswordMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(139, 82);
            // 
            // editUserMenuItem
            // 
            this.editUserMenuItem.Name = "editUserMenuItem";
            this.editUserMenuItem.Size = new System.Drawing.Size(138, 24);
            this.editUserMenuItem.Text = "编辑用户";
            this.editUserMenuItem.Click += new System.EventHandler(this.editUserMenuItem_Click);
            // 
            // deleteUserMenuItem
            // 
            this.deleteUserMenuItem.Name = "deleteUserMenuItem";
            this.deleteUserMenuItem.Size = new System.Drawing.Size(138, 24);
            this.deleteUserMenuItem.Text = "删除用户";
            this.deleteUserMenuItem.Click += new System.EventHandler(this.deleteUserMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(135, 6);
            // 
            // resetPasswordMenuItem
            // 
            this.resetPasswordMenuItem.Name = "resetPasswordMenuItem";
            this.resetPasswordMenuItem.Size = new System.Drawing.Size(138, 24);
            this.resetPasswordMenuItem.Text = "重置密码";
            this.resetPasswordMenuItem.Click += new System.EventHandler(this.resetPasswordMenuItem_Click);
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.addUserButton);
            this.topPanel.Controls.Add(this.titleLabel);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(27, 25);
            this.topPanel.Margin = new System.Windows.Forms.Padding(4);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(1226, 81);
            this.topPanel.TabIndex = 3;
            // 
            // addUserButton
            // 
            this.addUserButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.addUserButton.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Bold);
            this.addUserButton.Location = new System.Drawing.Point(999, 15);
            this.addUserButton.Margin = new System.Windows.Forms.Padding(4);
            this.addUserButton.Name = "addUserButton";
            this.addUserButton.Size = new System.Drawing.Size(160, 50);
            this.addUserButton.TabIndex = 3;
            this.addUserButton.Text = "➕ 添加用户";
            this.addUserButton.Type = AntdUI.TTypeMini.Primary;
            this.addUserButton.Click += new System.EventHandler(this.addUserButton_Click);
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(13, 15);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(4);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(267, 50);
            this.titleLabel.TabIndex = 2;
            this.titleLabel.Text = "用户管理";
            // 
            // UserManagementControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.userListView);
            this.Controls.Add(this.topPanel);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UserManagementControl";
            this.Padding = new System.Windows.Forms.Padding(27, 25, 27, 25);
            this.Size = new System.Drawing.Size(1280, 750);
            this.contextMenuStrip.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListView userListView;
        private System.Windows.Forms.ColumnHeader usernameHeader;
        private System.Windows.Forms.ColumnHeader permissionHeader;
        private System.Windows.Forms.ColumnHeader createdTimeHeader;
        private System.Windows.Forms.ColumnHeader lastLoginHeader;
        private System.Windows.Forms.ColumnHeader statusHeader;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem editUserMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteUserMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem resetPasswordMenuItem;
        private System.Windows.Forms.Panel topPanel;
        private AntdUI.Button addUserButton;
        private AntdUI.Label titleLabel;
    }
}
