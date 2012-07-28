namespace Crystalbyte.Equinox.DemoApp
{
    partial class ApplicationForm
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
            this.components = new System.ComponentModel.Container();
            this.MainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.MailboxTreeView = new System.Windows.Forms.TreeView();
            this.NestedSplitContainer = new System.Windows.Forms.SplitContainer();
            this.SideSplitContainer = new System.Windows.Forms.SplitContainer();
            this.AttachmentSplitContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.MessageProgressBar = new System.Windows.Forms.ProgressBar();
            this.MessageGrid = new System.Windows.Forms.DataGridView();
            this.subjectDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dateDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fromDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.messageBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.AttachmentListView = new System.Windows.Forms.ListView();
            this.MessageViewer = new System.Windows.Forms.WebBrowser();
            this.OutputTextBox = new System.Windows.Forms.TextBox();
            this.ApplicationMenuStrip = new System.Windows.Forms.MenuStrip();
            this.AccountsStripItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CreateAccountButton = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).BeginInit();
            this.MainSplitContainer.Panel1.SuspendLayout();
            this.MainSplitContainer.Panel2.SuspendLayout();
            this.MainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NestedSplitContainer)).BeginInit();
            this.NestedSplitContainer.Panel1.SuspendLayout();
            this.NestedSplitContainer.Panel2.SuspendLayout();
            this.NestedSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SideSplitContainer)).BeginInit();
            this.SideSplitContainer.Panel1.SuspendLayout();
            this.SideSplitContainer.Panel2.SuspendLayout();
            this.SideSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AttachmentSplitContainer)).BeginInit();
            this.AttachmentSplitContainer.Panel1.SuspendLayout();
            this.AttachmentSplitContainer.Panel2.SuspendLayout();
            this.AttachmentSplitContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MessageGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageBindingSource)).BeginInit();
            this.ApplicationMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainSplitContainer
            // 
            this.MainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplitContainer.Location = new System.Drawing.Point(0, 24);
            this.MainSplitContainer.Name = "MainSplitContainer";
            // 
            // MainSplitContainer.Panel1
            // 
            this.MainSplitContainer.Panel1.Controls.Add(this.MailboxTreeView);
            // 
            // MainSplitContainer.Panel2
            // 
            this.MainSplitContainer.Panel2.Controls.Add(this.NestedSplitContainer);
            this.MainSplitContainer.Size = new System.Drawing.Size(1008, 706);
            this.MainSplitContainer.SplitterDistance = 235;
            this.MainSplitContainer.TabIndex = 0;
            // 
            // MailboxTreeView
            // 
            this.MailboxTreeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MailboxTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MailboxTreeView.Location = new System.Drawing.Point(0, 0);
            this.MailboxTreeView.Margin = new System.Windows.Forms.Padding(0);
            this.MailboxTreeView.Name = "MailboxTreeView";
            this.MailboxTreeView.Size = new System.Drawing.Size(235, 706);
            this.MailboxTreeView.TabIndex = 0;
            this.MailboxTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.OnMailboxTreeViewNodeMouseClick);
            // 
            // NestedSplitContainer
            // 
            this.NestedSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.NestedSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.NestedSplitContainer.Margin = new System.Windows.Forms.Padding(0);
            this.NestedSplitContainer.Name = "NestedSplitContainer";
            this.NestedSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // NestedSplitContainer.Panel1
            // 
            this.NestedSplitContainer.Panel1.Controls.Add(this.SideSplitContainer);
            // 
            // NestedSplitContainer.Panel2
            // 
            this.NestedSplitContainer.Panel2.Controls.Add(this.OutputTextBox);
            this.NestedSplitContainer.Size = new System.Drawing.Size(769, 706);
            this.NestedSplitContainer.SplitterDistance = 641;
            this.NestedSplitContainer.TabIndex = 0;
            // 
            // SideSplitContainer
            // 
            this.SideSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SideSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.SideSplitContainer.Name = "SideSplitContainer";
            this.SideSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // SideSplitContainer.Panel1
            // 
            this.SideSplitContainer.Panel1.Controls.Add(this.AttachmentSplitContainer);
            // 
            // SideSplitContainer.Panel2
            // 
            this.SideSplitContainer.Panel2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.SideSplitContainer.Panel2.Controls.Add(this.MessageViewer);
            this.SideSplitContainer.Size = new System.Drawing.Size(769, 641);
            this.SideSplitContainer.SplitterDistance = 428;
            this.SideSplitContainer.TabIndex = 1;
            // 
            // AttachmentSplitContainer
            // 
            this.AttachmentSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AttachmentSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.AttachmentSplitContainer.Name = "AttachmentSplitContainer";
            // 
            // AttachmentSplitContainer.Panel1
            // 
            this.AttachmentSplitContainer.Panel1.Controls.Add(this.panel1);
            // 
            // AttachmentSplitContainer.Panel2
            // 
            this.AttachmentSplitContainer.Panel2.Controls.Add(this.AttachmentListView);
            this.AttachmentSplitContainer.Size = new System.Drawing.Size(769, 428);
            this.AttachmentSplitContainer.SplitterDistance = 602;
            this.AttachmentSplitContainer.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.MessageProgressBar);
            this.panel1.Controls.Add(this.MessageGrid);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(602, 428);
            this.panel1.TabIndex = 1;
            // 
            // MessageProgressBar
            // 
            this.MessageProgressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.MessageProgressBar.Location = new System.Drawing.Point(0, 405);
            this.MessageProgressBar.Margin = new System.Windows.Forms.Padding(3, 1, 3, 3);
            this.MessageProgressBar.Name = "MessageProgressBar";
            this.MessageProgressBar.Size = new System.Drawing.Size(602, 23);
            this.MessageProgressBar.TabIndex = 1;
            // 
            // MessageGrid
            // 
            this.MessageGrid.AllowUserToAddRows = false;
            this.MessageGrid.AllowUserToDeleteRows = false;
            this.MessageGrid.AllowUserToOrderColumns = true;
            this.MessageGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageGrid.AutoGenerateColumns = false;
            this.MessageGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.MessageGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.subjectDataGridViewTextBoxColumn,
            this.dateDataGridViewTextBoxColumn,
            this.fromDataGridViewTextBoxColumn,
            this.toDataGridViewTextBoxColumn});
            this.MessageGrid.DataSource = this.messageBindingSource;
            this.MessageGrid.Location = new System.Drawing.Point(0, 0);
            this.MessageGrid.Margin = new System.Windows.Forms.Padding(0);
            this.MessageGrid.MultiSelect = false;
            this.MessageGrid.Name = "MessageGrid";
            this.MessageGrid.ReadOnly = true;
            this.MessageGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.MessageGrid.Size = new System.Drawing.Size(602, 404);
            this.MessageGrid.TabIndex = 0;
            this.MessageGrid.SelectionChanged += new System.EventHandler(this.OnMessageGridSelectionChanged);
            // 
            // subjectDataGridViewTextBoxColumn
            // 
            this.subjectDataGridViewTextBoxColumn.DataPropertyName = "Subject";
            this.subjectDataGridViewTextBoxColumn.HeaderText = "Subject";
            this.subjectDataGridViewTextBoxColumn.Name = "subjectDataGridViewTextBoxColumn";
            this.subjectDataGridViewTextBoxColumn.ReadOnly = true;
            this.subjectDataGridViewTextBoxColumn.Width = 300;
            // 
            // dateDataGridViewTextBoxColumn
            // 
            this.dateDataGridViewTextBoxColumn.DataPropertyName = "Date";
            this.dateDataGridViewTextBoxColumn.HeaderText = "Date";
            this.dateDataGridViewTextBoxColumn.Name = "dateDataGridViewTextBoxColumn";
            this.dateDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // fromDataGridViewTextBoxColumn
            // 
            this.fromDataGridViewTextBoxColumn.DataPropertyName = "From";
            this.fromDataGridViewTextBoxColumn.HeaderText = "From";
            this.fromDataGridViewTextBoxColumn.Name = "fromDataGridViewTextBoxColumn";
            this.fromDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // toDataGridViewTextBoxColumn
            // 
            this.toDataGridViewTextBoxColumn.DataPropertyName = "To";
            this.toDataGridViewTextBoxColumn.HeaderText = "To";
            this.toDataGridViewTextBoxColumn.Name = "toDataGridViewTextBoxColumn";
            this.toDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // messageBindingSource
            // 
            this.messageBindingSource.DataSource = typeof(Crystalbyte.Equinox.DemoApp.MyMessage);
            // 
            // AttachmentListView
            // 
            this.AttachmentListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.AttachmentListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AttachmentListView.Location = new System.Drawing.Point(0, 0);
            this.AttachmentListView.Name = "AttachmentListView";
            this.AttachmentListView.Size = new System.Drawing.Size(163, 428);
            this.AttachmentListView.TabIndex = 0;
            this.AttachmentListView.UseCompatibleStateImageBehavior = false;
            this.AttachmentListView.DoubleClick += new System.EventHandler(this.OnListViewItemDoubleClicked);
            // 
            // MessageViewer
            // 
            this.MessageViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageViewer.Location = new System.Drawing.Point(0, 0);
            this.MessageViewer.MinimumSize = new System.Drawing.Size(20, 20);
            this.MessageViewer.Name = "MessageViewer";
            this.MessageViewer.Size = new System.Drawing.Size(769, 209);
            this.MessageViewer.TabIndex = 0;
            // 
            // OutputTextBox
            // 
            this.OutputTextBox.BackColor = System.Drawing.SystemColors.ControlLight;
            this.OutputTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.OutputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OutputTextBox.Location = new System.Drawing.Point(0, 0);
            this.OutputTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.OutputTextBox.Multiline = true;
            this.OutputTextBox.Name = "OutputTextBox";
            this.OutputTextBox.ReadOnly = true;
            this.OutputTextBox.Size = new System.Drawing.Size(769, 61);
            this.OutputTextBox.TabIndex = 0;
            // 
            // ApplicationMenuStrip
            // 
            this.ApplicationMenuStrip.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ApplicationMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AccountsStripItem});
            this.ApplicationMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.ApplicationMenuStrip.Name = "ApplicationMenuStrip";
            this.ApplicationMenuStrip.Size = new System.Drawing.Size(1008, 24);
            this.ApplicationMenuStrip.TabIndex = 1;
            this.ApplicationMenuStrip.Text = "menuStrip1";
            // 
            // AccountsStripItem
            // 
            this.AccountsStripItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CreateAccountButton});
            this.AccountsStripItem.Name = "AccountsStripItem";
            this.AccountsStripItem.Size = new System.Drawing.Size(69, 20);
            this.AccountsStripItem.Text = "Accounts";
            // 
            // CreateAccountButton
            // 
            this.CreateAccountButton.Name = "CreateAccountButton";
            this.CreateAccountButton.Size = new System.Drawing.Size(108, 22);
            this.CreateAccountButton.Text = "Create";
            this.CreateAccountButton.Click += new System.EventHandler(this.CreateAccountButtonClick);
            // 
            // ApplicationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 730);
            this.Controls.Add(this.MainSplitContainer);
            this.Controls.Add(this.ApplicationMenuStrip);
            this.MainMenuStrip = this.ApplicationMenuStrip;
            this.Name = "ApplicationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Crystalbyte Equinox Imap Demo App";
            this.MainSplitContainer.Panel1.ResumeLayout(false);
            this.MainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitContainer)).EndInit();
            this.MainSplitContainer.ResumeLayout(false);
            this.NestedSplitContainer.Panel1.ResumeLayout(false);
            this.NestedSplitContainer.Panel2.ResumeLayout(false);
            this.NestedSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NestedSplitContainer)).EndInit();
            this.NestedSplitContainer.ResumeLayout(false);
            this.SideSplitContainer.Panel1.ResumeLayout(false);
            this.SideSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SideSplitContainer)).EndInit();
            this.SideSplitContainer.ResumeLayout(false);
            this.AttachmentSplitContainer.Panel1.ResumeLayout(false);
            this.AttachmentSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.AttachmentSplitContainer)).EndInit();
            this.AttachmentSplitContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MessageGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageBindingSource)).EndInit();
            this.ApplicationMenuStrip.ResumeLayout(false);
            this.ApplicationMenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer MainSplitContainer;
        private System.Windows.Forms.TreeView MailboxTreeView;
        private System.Windows.Forms.MenuStrip ApplicationMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem AccountsStripItem;
        private System.Windows.Forms.ToolStripMenuItem CreateAccountButton;
        private System.Windows.Forms.SplitContainer NestedSplitContainer;
        private System.Windows.Forms.SplitContainer SideSplitContainer;
        private System.Windows.Forms.TextBox OutputTextBox;
        private System.Windows.Forms.DataGridView MessageGrid;
        private System.Windows.Forms.BindingSource messageBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn subjectDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dateDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn fromDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn toDataGridViewTextBoxColumn;
        private System.Windows.Forms.SplitContainer AttachmentSplitContainer;
        private System.Windows.Forms.ListView AttachmentListView;
        private System.Windows.Forms.WebBrowser MessageViewer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ProgressBar MessageProgressBar;
    }
}

