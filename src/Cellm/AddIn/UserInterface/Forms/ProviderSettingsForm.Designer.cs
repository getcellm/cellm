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
        this.apiKeyLabel = new System.Windows.Forms.Label();
        this.apiKeyTextBox = new System.Windows.Forms.TextBox();
        this.baseAddressLabel = new System.Windows.Forms.Label();
        this.baseAddressTextBox = new System.Windows.Forms.TextBox();
        this.okButton = new System.Windows.Forms.Button();
        this.cancelButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // apiKeyLabel
        //
        this.apiKeyLabel.AutoSize = true;
        this.apiKeyLabel.Location = new System.Drawing.Point(12, 15);
        this.apiKeyLabel.Name = "apiKeyLabel";
        this.apiKeyLabel.Size = new System.Drawing.Size(57, 13);
        this.apiKeyLabel.TabIndex = 0;
        this.apiKeyLabel.Text = "API Key:";
        //
        // apiKeyTextBox
        //
        this.apiKeyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.apiKeyTextBox.Location = new System.Drawing.Point(15, 31);
        this.apiKeyTextBox.Name = "apiKeyTextBox";
        this.apiKeyTextBox.Size = new System.Drawing.Size(357, 20);
        this.apiKeyTextBox.TabIndex = 0; // TabIndex 0
        this.apiKeyTextBox.UseSystemPasswordChar = true;
        //
        // baseAddressLabel
        //
        this.baseAddressLabel.AutoSize = true;
        this.baseAddressLabel.Location = new System.Drawing.Point(12, 65); // Position below ApiKey
        this.baseAddressLabel.Name = "baseAddressLabel";
        this.baseAddressLabel.Size = new System.Drawing.Size(80, 13);
        this.baseAddressLabel.TabIndex = 2;
        this.baseAddressLabel.Text = "Base Address:";
        //
        // baseAddressTextBox
        //
        this.baseAddressTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.baseAddressTextBox.Location = new System.Drawing.Point(15, 81); // Position below Base Address Label
        this.baseAddressTextBox.Name = "baseAddressTextBox";
        this.baseAddressTextBox.Size = new System.Drawing.Size(357, 20);
        this.baseAddressTextBox.TabIndex = 1; // TabIndex 1
        //
        // okButton
        //
        this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.okButton.Location = new System.Drawing.Point(216, 117); // Position bottom right
        this.okButton.Name = "okButton";
        this.okButton.Size = new System.Drawing.Size(75, 23);
        this.okButton.TabIndex = 2; // TabIndex 2
        this.okButton.Text = "OK";
        this.okButton.UseVisualStyleBackColor = true;
        this.okButton.Click += new System.EventHandler(this.okButton_Click);
        //
        // cancelButton
        //
        this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.cancelButton.Location = new System.Drawing.Point(297, 117); // Position bottom right
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.Size = new System.Drawing.Size(75, 23);
        this.cancelButton.TabIndex = 3; // TabIndex 3
        this.cancelButton.Text = "Cancel";
        this.cancelButton.UseVisualStyleBackColor = true;
        this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
        //
        // ProviderSettingsForm
        //
        this.AcceptButton = this.okButton;
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.cancelButton;
        this.ClientSize = new System.Drawing.Size(384, 152); // Adjust size as needed
        this.Controls.Add(this.cancelButton);
        this.Controls.Add(this.okButton);
        this.Controls.Add(this.baseAddressTextBox);
        this.Controls.Add(this.baseAddressLabel);
        this.Controls.Add(this.apiKeyTextBox);
        this.Controls.Add(this.apiKeyLabel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ProviderSettingsForm";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Provider Settings"; // Will be overridden in constructor
        this.ResumeLayout(false);
        this.PerformLayout();

    }
    #endregion
}
