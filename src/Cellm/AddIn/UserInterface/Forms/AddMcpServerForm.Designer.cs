namespace Cellm.AddIn.UserInterface.Forms;

partial class AddMcpServerForm
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
        transportTypeLabel = new Label();
        transportTypeComboBox = new ComboBox();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        commandLabel = new Label();
        commandTextBox = new TextBox();
        argumentsLabel = new Label();
        argumentsTextBox = new TextBox();
        environmentVariablesLabel = new Label();
        environmentVariablesButton = new Button();
        endpointLabel = new Label();
        endpointTextBox = new TextBox();
        transportModeLabel = new Label();
        httpTransportModeComboBox = new ComboBox();
        connectionTimeoutLabel = new Label();
        connectionTimeoutNumericUpDown = new NumericUpDown();
        additionalHeadersLabel = new Label();
        additionalHeadersButton = new Button();
        okButton = new Button();
        cancelButton = new Button();
        ((System.ComponentModel.ISupportInitialize)connectionTimeoutNumericUpDown).BeginInit();
        SuspendLayout();
        // 
        // transportTypeLabel
        // 
        transportTypeLabel.AutoSize = true;
        transportTypeLabel.Location = new Point(12, 15);
        transportTypeLabel.Name = "transportTypeLabel";
        transportTypeLabel.Size = new Size(86, 15);
        transportTypeLabel.TabIndex = 0;
        transportTypeLabel.Text = "Transport Type:";
        // 
        // transportTypeComboBox
        // 
        transportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        transportTypeComboBox.FormattingEnabled = true;
        transportTypeComboBox.Location = new Point(111, 12);
        transportTypeComboBox.Name = "transportTypeComboBox";
        transportTypeComboBox.Size = new Size(194, 23);
        transportTypeComboBox.TabIndex = 1;
        transportTypeComboBox.SelectedIndexChanged += transportTypeComboBox_SelectedIndexChanged;
        // 
        // nameLabel
        // 
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(12, 50);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(42, 15);
        nameLabel.TabIndex = 2;
        nameLabel.Text = "Name:";
        // 
        // nameTextBox
        // 
        nameTextBox.Location = new Point(111, 47);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(194, 23);
        nameTextBox.TabIndex = 3;
        // 
        // commandLabel
        // 
        commandLabel.AutoSize = true;
        commandLabel.Location = new Point(12, 85);
        commandLabel.Name = "commandLabel";
        commandLabel.Size = new Size(67, 15);
        commandLabel.TabIndex = 4;
        commandLabel.Text = "Command:";
        // 
        // commandTextBox
        // 
        commandTextBox.Location = new Point(111, 82);
        commandTextBox.Name = "commandTextBox";
        commandTextBox.Size = new Size(194, 23);
        commandTextBox.TabIndex = 5;
        // 
        // argumentsLabel
        // 
        argumentsLabel.AutoSize = true;
        argumentsLabel.Location = new Point(12, 120);
        argumentsLabel.Name = "argumentsLabel";
        argumentsLabel.Size = new Size(69, 15);
        argumentsLabel.TabIndex = 6;
        argumentsLabel.Text = "Arguments:";
        // 
        // argumentsTextBox
        // 
        argumentsTextBox.Location = new Point(111, 117);
        argumentsTextBox.Name = "argumentsTextBox";
        argumentsTextBox.Size = new Size(194, 23);
        argumentsTextBox.TabIndex = 7;
        // 
        // environmentVariablesLabel
        // 
        environmentVariablesLabel.AutoSize = true;
        environmentVariablesLabel.Location = new Point(12, 155);
        environmentVariablesLabel.Name = "environmentVariablesLabel";
        environmentVariablesLabel.Size = new Size(127, 15);
        environmentVariablesLabel.TabIndex = 8;
        environmentVariablesLabel.Text = "Environment Variables:";
        // 
        // environmentVariablesButton
        // 
        environmentVariablesButton.Location = new Point(157, 151);
        environmentVariablesButton.Name = "environmentVariablesButton";
        environmentVariablesButton.Size = new Size(148, 23);
        environmentVariablesButton.TabIndex = 9;
        environmentVariablesButton.Text = "Edit...";
        environmentVariablesButton.UseVisualStyleBackColor = true;
        environmentVariablesButton.Click += EnvironmentVariablesButton_Click;
        // 
        // endpointLabel
        // 
        endpointLabel.AutoSize = true;
        endpointLabel.Location = new Point(12, 85);
        endpointLabel.Name = "endpointLabel";
        endpointLabel.Size = new Size(58, 15);
        endpointLabel.TabIndex = 14;
        endpointLabel.Text = "Endpoint:";
        // 
        // endpointTextBox
        // 
        endpointTextBox.Location = new Point(111, 82);
        endpointTextBox.Name = "endpointTextBox";
        endpointTextBox.Size = new Size(194, 23);
        endpointTextBox.TabIndex = 15;
        // 
        // transportModeLabel
        // 
        transportModeLabel.AutoSize = true;
        transportModeLabel.Location = new Point(12, 120);
        transportModeLabel.Name = "transportModeLabel";
        transportModeLabel.Size = new Size(93, 15);
        transportModeLabel.TabIndex = 16;
        transportModeLabel.Text = "Transport Mode:";
        // 
        // httpTransportModeComboBox
        // 
        httpTransportModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        httpTransportModeComboBox.FormattingEnabled = true;
        httpTransportModeComboBox.Location = new Point(111, 117);
        httpTransportModeComboBox.Name = "httpTransportModeComboBox";
        httpTransportModeComboBox.Size = new Size(194, 23);
        httpTransportModeComboBox.TabIndex = 17;
        // 
        // connectionTimeoutLabel
        // 
        connectionTimeoutLabel.AutoSize = true;
        connectionTimeoutLabel.Location = new Point(12, 155);
        connectionTimeoutLabel.Name = "connectionTimeoutLabel";
        connectionTimeoutLabel.Size = new Size(135, 15);
        connectionTimeoutLabel.TabIndex = 18;
        connectionTimeoutLabel.Text = "Connection Timeout (s):";
        // 
        // connectionTimeoutNumericUpDown
        // 
        connectionTimeoutNumericUpDown.Location = new Point(156, 153);
        connectionTimeoutNumericUpDown.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        connectionTimeoutNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        connectionTimeoutNumericUpDown.Name = "connectionTimeoutNumericUpDown";
        connectionTimeoutNumericUpDown.Size = new Size(149, 23);
        connectionTimeoutNumericUpDown.TabIndex = 19;
        connectionTimeoutNumericUpDown.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // additionalHeadersLabel
        // 
        additionalHeadersLabel.AutoSize = true;
        additionalHeadersLabel.Location = new Point(12, 190);
        additionalHeadersLabel.Name = "additionalHeadersLabel";
        additionalHeadersLabel.Size = new Size(111, 15);
        additionalHeadersLabel.TabIndex = 14;
        additionalHeadersLabel.Text = "Additional Headers:";
        // 
        // additionalHeadersButton
        // 
        additionalHeadersButton.Location = new Point(127, 186);
        additionalHeadersButton.Name = "additionalHeadersButton";
        additionalHeadersButton.Size = new Size(178, 23);
        additionalHeadersButton.TabIndex = 15;
        additionalHeadersButton.Text = "Edit...";
        additionalHeadersButton.UseVisualStyleBackColor = true;
        additionalHeadersButton.Click += AdditionalHeadersButton_Click;
        // 
        // okButton
        // 
        okButton.Location = new Point(149, 230);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 23);
        okButton.TabIndex = 16;
        okButton.Text = "OK";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += OkButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(230, 230);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 23);
        cancelButton.TabIndex = 17;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // AddMcpServerForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(317, 294);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(additionalHeadersButton);
        Controls.Add(additionalHeadersLabel);
        Controls.Add(connectionTimeoutNumericUpDown);
        Controls.Add(connectionTimeoutLabel);
        Controls.Add(httpTransportModeComboBox);
        Controls.Add(transportModeLabel);
        Controls.Add(endpointTextBox);
        Controls.Add(endpointLabel);
        Controls.Add(environmentVariablesButton);
        Controls.Add(environmentVariablesLabel);
        Controls.Add(argumentsTextBox);
        Controls.Add(argumentsLabel);
        Controls.Add(commandTextBox);
        Controls.Add(commandLabel);
        Controls.Add(nameTextBox);
        Controls.Add(nameLabel);
        Controls.Add(transportTypeComboBox);
        Controls.Add(transportTypeLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AddMcpServerForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add MCP Server";
        Load += AddMcpServerForm_Load;
        ((System.ComponentModel.ISupportInitialize)connectionTimeoutNumericUpDown).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label transportTypeLabel;
    private ComboBox transportTypeComboBox;
    private Label nameLabel;
    private TextBox nameTextBox;
    private Label commandLabel;
    private TextBox commandTextBox;
    private Label argumentsLabel;
    private TextBox argumentsTextBox;
    private Label environmentVariablesLabel;
    private Button environmentVariablesButton;
    private Label endpointLabel;
    private TextBox endpointTextBox;
    private Label transportModeLabel;
    private ComboBox httpTransportModeComboBox;
    private Label connectionTimeoutLabel;
    private NumericUpDown connectionTimeoutNumericUpDown;
    private Label additionalHeadersLabel;
    private Button additionalHeadersButton;
    private Button okButton;
    private Button cancelButton;
}