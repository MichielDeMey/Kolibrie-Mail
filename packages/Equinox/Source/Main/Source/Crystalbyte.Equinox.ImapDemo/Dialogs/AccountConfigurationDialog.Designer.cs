namespace Crystalbyte.Equinox.ImapDemo.Dialogs
{
    partial class AccountConfigurationDialog
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
            this.AccountList = new System.Windows.Forms.ListView();
            this.AddButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.CloseButton = new System.Windows.Forms.Button();
            this.AccountSettingsTabControl = new System.Windows.Forms.TabControl();
            this.GeneralAccountTabPage = new System.Windows.Forms.TabPage();
            this.AccountServerSettingsTabPage = new System.Windows.Forms.TabPage();
            this._label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this._label3 = new System.Windows.Forms.Label();
            this.HostTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.EncryptionListBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.AccountSettingsTabControl.SuspendLayout();
            this.GeneralAccountTabPage.SuspendLayout();
            this.AccountServerSettingsTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // AccountList
            // 
            this.AccountList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.AccountList.Location = new System.Drawing.Point(12, 12);
            this.AccountList.Name = "AccountList";
            this.AccountList.Size = new System.Drawing.Size(156, 262);
            this.AccountList.TabIndex = 0;
            this.AccountList.UseCompatibleStateImageBehavior = false;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(12, 280);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 1;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(93, 280);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveButton.TabIndex = 1;
            this.RemoveButton.Text = "Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            // 
            // CloseButton
            // 
            this.CloseButton.Location = new System.Drawing.Point(535, 280);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 1;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            // 
            // AccountSettingsTabControl
            // 
            this.AccountSettingsTabControl.Controls.Add(this.GeneralAccountTabPage);
            this.AccountSettingsTabControl.Controls.Add(this.AccountServerSettingsTabPage);
            this.AccountSettingsTabControl.Location = new System.Drawing.Point(174, 12);
            this.AccountSettingsTabControl.Name = "AccountSettingsTabControl";
            this.AccountSettingsTabControl.SelectedIndex = 0;
            this.AccountSettingsTabControl.Size = new System.Drawing.Size(436, 262);
            this.AccountSettingsTabControl.TabIndex = 2;
            // 
            // GeneralAccountTabPage
            // 
            this.GeneralAccountTabPage.Controls.Add(this.textBox1);
            this.GeneralAccountTabPage.Controls.Add(this._label1);
            this.GeneralAccountTabPage.Location = new System.Drawing.Point(4, 22);
            this.GeneralAccountTabPage.Name = "GeneralAccountTabPage";
            this.GeneralAccountTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GeneralAccountTabPage.Size = new System.Drawing.Size(428, 236);
            this.GeneralAccountTabPage.TabIndex = 0;
            this.GeneralAccountTabPage.Text = "General";
            this.GeneralAccountTabPage.UseVisualStyleBackColor = true;
            // 
            // AccountServerSettingsTabPage
            // 
            this.AccountServerSettingsTabPage.Controls.Add(this.EncryptionListBox);
            this.AccountServerSettingsTabPage.Controls.Add(this.label1);
            this.AccountServerSettingsTabPage.Controls.Add(this.PasswordTextBox);
            this.AccountServerSettingsTabPage.Controls.Add(this.UsernameTextBox);
            this.AccountServerSettingsTabPage.Controls.Add(this.label3);
            this.AccountServerSettingsTabPage.Controls.Add(this.HostTextBox);
            this.AccountServerSettingsTabPage.Controls.Add(this.label2);
            this.AccountServerSettingsTabPage.Controls.Add(this._label3);
            this.AccountServerSettingsTabPage.Location = new System.Drawing.Point(4, 22);
            this.AccountServerSettingsTabPage.Name = "AccountServerSettingsTabPage";
            this.AccountServerSettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.AccountServerSettingsTabPage.Size = new System.Drawing.Size(428, 236);
            this.AccountServerSettingsTabPage.TabIndex = 1;
            this.AccountServerSettingsTabPage.Text = "Connection";
            this.AccountServerSettingsTabPage.UseVisualStyleBackColor = true;
            // 
            // _label1
            // 
            this._label1.AutoSize = true;
            this._label1.Location = new System.Drawing.Point(18, 21);
            this._label1.Name = "_label1";
            this._label1.Size = new System.Drawing.Size(81, 13);
            this._label1.TabIndex = 0;
            this._label1.Text = "Account Name:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(105, 18);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(305, 20);
            this.textBox1.TabIndex = 1;
            // 
            // _label3
            // 
            this._label3.AutoSize = true;
            this._label3.Location = new System.Drawing.Point(20, 19);
            this._label3.Name = "_label3";
            this._label3.Size = new System.Drawing.Size(32, 13);
            this._label3.TabIndex = 0;
            this._label3.Text = "Host:";
            // 
            // HostTextBox
            // 
            this.HostTextBox.Location = new System.Drawing.Point(58, 16);
            this.HostTextBox.Name = "HostTextBox";
            this.HostTextBox.Size = new System.Drawing.Size(349, 20);
            this.HostTextBox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Encryption:";
            // 
            // EncryptionListBox
            // 
            this.EncryptionListBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EncryptionListBox.FormattingEnabled = true;
            this.EncryptionListBox.Items.AddRange(new object[] {
            "Implicit (TLS)",
            "Explicit (SSL)"});
            this.EncryptionListBox.Location = new System.Drawing.Point(86, 42);
            this.EncryptionListBox.Name = "EncryptionListBox";
            this.EncryptionListBox.Size = new System.Drawing.Size(321, 21);
            this.EncryptionListBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 97);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Username:";
            // 
            // UsernameTextBox
            // 
            this.UsernameTextBox.Location = new System.Drawing.Point(84, 94);
            this.UsernameTextBox.Name = "UsernameTextBox";
            this.UsernameTextBox.Size = new System.Drawing.Size(323, 20);
            this.UsernameTextBox.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 123);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Password:";
            // 
            // PasswordTextBox
            // 
            this.PasswordTextBox.Location = new System.Drawing.Point(84, 120);
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.Size = new System.Drawing.Size(323, 20);
            this.PasswordTextBox.TabIndex = 1;
            this.PasswordTextBox.UseSystemPasswordChar = true;
            // 
            // AccountConfigurationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(622, 314);
            this.Controls.Add(this.AccountSettingsTabControl);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.AccountList);
            this.Name = "AccountConfigurationDialog";
            this.Text = "AccountConfigurationDialog";
            this.AccountSettingsTabControl.ResumeLayout(false);
            this.GeneralAccountTabPage.ResumeLayout(false);
            this.GeneralAccountTabPage.PerformLayout();
            this.AccountServerSettingsTabPage.ResumeLayout(false);
            this.AccountServerSettingsTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView AccountList;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.TabControl AccountSettingsTabControl;
        private System.Windows.Forms.TabPage GeneralAccountTabPage;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.TabPage AccountServerSettingsTabPage;
        private System.Windows.Forms.ComboBox EncryptionListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox HostTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label _label3;
    }
}