namespace Cellm.AddIn.UserInterface.Forms;

partial class ProviderSettingsForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.Windows.Forms.Label apiKeyLabel;
    private System.Windows.Forms.TextBox apiKeyTextBox;
    private System.Windows.Forms.Label baseAddressLabel;
    private System.Windows.Forms.TextBox baseAddressTextBox;
    private System.Windows.Forms.Button okButton;
    private System.Windows.Forms.Button cancelButton;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        apiKeyLabel = new Label();
        apiKeyTextBox = new TextBox();
        baseAddressLabel = new Label();
        baseAddressTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();
        // 
        // apiKeyLabel
        // 
        apiKeyLabel.AutoSize = true;
        apiKeyLabel.Location = new Point(14, 17);
        apiKeyLabel.Margin = new Padding(4, 0, 4, 0);
        apiKeyLabel.Name = "apiKeyLabel";
        apiKeyLabel.Size = new Size(50, 15);
        apiKeyLabel.TabIndex = 0;
        apiKeyLabel.Text = "API Key:";
        // 
        // apiKeyTextBox
        // 
        apiKeyTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        apiKeyTextBox.Location = new Point(101, 15);
        apiKeyTextBox.Margin = new Padding(4, 3, 4, 3);
        apiKeyTextBox.Name = "apiKeyTextBox";
        apiKeyTextBox.Size = new Size(218, 23);
        apiKeyTextBox.TabIndex = 0;
        apiKeyTextBox.UseSystemPasswordChar = true;
        // 
        // baseAddressLabel
        // 
        baseAddressLabel.AutoSize = true;
        baseAddressLabel.Location = new Point(14, 47);
        baseAddressLabel.Margin = new Padding(4, 0, 4, 0);
        baseAddressLabel.Name = "baseAddressLabel";
        baseAddressLabel.Size = new Size(79, 15);
        baseAddressLabel.TabIndex = 2;
        baseAddressLabel.Text = "Base Address:";
        // 
        // baseAddressTextBox
        // 
        baseAddressTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        baseAddressTextBox.Location = new Point(101, 44);
        baseAddressTextBox.Margin = new Padding(4, 3, 4, 3);
        baseAddressTextBox.Name = "baseAddressTextBox";
        baseAddressTextBox.Size = new Size(218, 23);
        baseAddressTextBox.TabIndex = 1;
        // 
        // okButton
        // 
        okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        okButton.DialogResult = DialogResult.OK;
        okButton.Location = new Point(137, 84);
        okButton.Margin = new Padding(4, 3, 4, 3);
        okButton.Name = "okButton";
        okButton.Size = new Size(88, 27);
        okButton.TabIndex = 2;
        okButton.Text = "OK";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += okButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(231, 84);
        cancelButton.Margin = new Padding(4, 3, 4, 3);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(88, 27);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // ProviderSettingsForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(333, 124);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(baseAddressTextBox);
        Controls.Add(baseAddressLabel);
        Controls.Add(apiKeyTextBox);
        Controls.Add(apiKeyLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProviderSettingsForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Provider Settings";
        Load += ProviderSettingsForm_Load;
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion
}
