namespace Cellm.AddIn.UserInterface.Forms;

partial class HeaderEditForm
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
        nameLabel = new Label();
        nameTextBox = new TextBox();
        valueLabel = new Label();
        valueTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();
        // 
        // nameLabel
        // 
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(12, 15);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(82, 15);
        nameLabel.TabIndex = 0;
        nameLabel.Text = "Header Name:";
        // 
        // nameTextBox
        // 
        nameTextBox.Location = new Point(100, 12);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(200, 23);
        nameTextBox.TabIndex = 1;
        // 
        // valueLabel
        // 
        valueLabel.AutoSize = true;
        valueLabel.Location = new Point(12, 47);
        valueLabel.Name = "valueLabel";
        valueLabel.Size = new Size(81, 15);
        valueLabel.TabIndex = 2;
        valueLabel.Text = "Header Value:";
        // 
        // valueTextBox
        // 
        valueTextBox.Location = new Point(100, 44);
        valueTextBox.Name = "valueTextBox";
        valueTextBox.Size = new Size(200, 23);
        valueTextBox.TabIndex = 3;
        // 
        // okButton
        // 
        okButton.Location = new Point(144, 85);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 23);
        okButton.TabIndex = 4;
        okButton.Text = "OK";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += okButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(225, 85);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 23);
        cancelButton.TabIndex = 5;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // HeaderEditForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(312, 120);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(valueTextBox);
        Controls.Add(valueLabel);
        Controls.Add(nameTextBox);
        Controls.Add(nameLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "HeaderEditForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit Header";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label nameLabel;
    private TextBox nameTextBox;
    private Label valueLabel;
    private TextBox valueTextBox;
    private Button okButton;
    private Button cancelButton;
}