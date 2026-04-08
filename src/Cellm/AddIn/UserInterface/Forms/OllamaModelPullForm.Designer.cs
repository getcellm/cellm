namespace Cellm.AddIn.UserInterface.Forms;

partial class OllamaModelPullForm
{
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Button cancelButton;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        statusLabel = new Label();
        progressBar = new ProgressBar();
        cancelButton = new Button();
        SuspendLayout();
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(14, 15);
        statusLabel.Margin = new Padding(4, 0, 4, 0);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(73, 15);
        statusLabel.TabIndex = 0;
        statusLabel.Text = "Downloading...";
        //
        // progressBar
        //
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(14, 40);
        progressBar.Margin = new Padding(4, 3, 4, 3);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(355, 23);
        progressBar.TabIndex = 1;
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(281, 76);
        cancelButton.Margin = new Padding(4, 3, 4, 3);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(88, 27);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        //
        // OllamaModelPullForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(383, 116);
        Controls.Add(cancelButton);
        Controls.Add(progressBar);
        Controls.Add(statusLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "OllamaModelPullForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Downloading Model";
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion
}
