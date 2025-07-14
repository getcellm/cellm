namespace Cellm.AddIn.UserInterface.Forms;

partial class EditMcpServerForm
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
        serverListBox = new ListBox();
        addButton = new Button();
        editButton = new Button();
        removeButton = new Button();
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
        serverListLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)connectionTimeoutNumericUpDown).BeginInit();
        SuspendLayout();
        // 
        // serverListBox
        // 
        serverListBox.FormattingEnabled = true;
        serverListBox.ItemHeight = 15;
        serverListBox.Location = new Point(12, 27);
        serverListBox.Name = "serverListBox";
        serverListBox.Size = new Size(200, 154);
        serverListBox.TabIndex = 0;
        serverListBox.SelectedIndexChanged += serverListBox_SelectedIndexChanged;
        // 
        // addButton
        // 
        addButton.Location = new Point(12, 190);
        addButton.Name = "addButton";
        addButton.Size = new Size(60, 23);
        addButton.TabIndex = 1;
        addButton.Text = "Add";
        addButton.UseVisualStyleBackColor = true;
        addButton.Click += addButton_Click;
        // 
        // editButton
        // 
        editButton.Enabled = false;
        editButton.Location = new Point(78, 190);
        editButton.Name = "editButton";
        editButton.Size = new Size(60, 23);
        editButton.TabIndex = 2;
        editButton.Text = "Edit";
        editButton.UseVisualStyleBackColor = true;
        editButton.Click += editButton_Click;
        // 
        // removeButton
        // 
        removeButton.Enabled = false;
        removeButton.Location = new Point(144, 190);
        removeButton.Name = "removeButton";
        removeButton.Size = new Size(68, 23);
        removeButton.TabIndex = 3;
        removeButton.Text = "Remove";
        removeButton.UseVisualStyleBackColor = true;
        removeButton.Click += removeButton_Click;
        // 
        // transportTypeLabel
        // 
        transportTypeLabel.AutoSize = true;
        transportTypeLabel.Location = new Point(230, 27);
        transportTypeLabel.Name = "transportTypeLabel";
        transportTypeLabel.Size = new Size(87, 15);
        transportTypeLabel.TabIndex = 4;
        transportTypeLabel.Text = "Transport Type:";
        // 
        // transportTypeComboBox
        // 
        transportTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        transportTypeComboBox.FormattingEnabled = true;
        transportTypeComboBox.Location = new Point(230, 45);
        transportTypeComboBox.Name = "transportTypeComboBox";
        transportTypeComboBox.Size = new Size(200, 23);
        transportTypeComboBox.TabIndex = 5;
        transportTypeComboBox.SelectedIndexChanged += transportTypeComboBox_SelectedIndexChanged;
        // 
        // nameLabel
        // 
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(230, 78);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(42, 15);
        nameLabel.TabIndex = 6;
        nameLabel.Text = "Name:";
        // 
        // nameTextBox
        // 
        nameTextBox.Location = new Point(230, 96);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(200, 23);
        nameTextBox.TabIndex = 7;
        // 
        // commandLabel
        // 
        commandLabel.AutoSize = true;
        commandLabel.Location = new Point(230, 129);
        commandLabel.Name = "commandLabel";
        commandLabel.Size = new Size(67, 15);
        commandLabel.TabIndex = 8;
        commandLabel.Text = "Command:";
        // 
        // commandTextBox
        // 
        commandTextBox.Location = new Point(230, 147);
        commandTextBox.Name = "commandTextBox";
        commandTextBox.Size = new Size(200, 23);
        commandTextBox.TabIndex = 9;
        // 
        // argumentsLabel
        // 
        argumentsLabel.AutoSize = true;
        argumentsLabel.Location = new Point(230, 180);
        argumentsLabel.Name = "argumentsLabel";
        argumentsLabel.Size = new Size(70, 15);
        argumentsLabel.TabIndex = 10;
        argumentsLabel.Text = "Arguments:";
        // 
        // argumentsTextBox
        // 
        argumentsTextBox.Location = new Point(230, 198);
        argumentsTextBox.Name = "argumentsTextBox";
        argumentsTextBox.Size = new Size(200, 23);
        argumentsTextBox.TabIndex = 11;
        // 
        // environmentVariablesLabel
        // 
        environmentVariablesLabel.AutoSize = true;
        environmentVariablesLabel.Location = new Point(230, 231);
        environmentVariablesLabel.Name = "environmentVariablesLabel";
        environmentVariablesLabel.Size = new Size(139, 15);
        environmentVariablesLabel.TabIndex = 12;
        environmentVariablesLabel.Text = "Environment Variables:";
        // 
        // environmentVariablesButton
        // 
        environmentVariablesButton.Location = new Point(230, 249);
        environmentVariablesButton.Name = "environmentVariablesButton";
        environmentVariablesButton.Size = new Size(200, 23);
        environmentVariablesButton.TabIndex = 13;
        environmentVariablesButton.Text = "Edit...";
        environmentVariablesButton.UseVisualStyleBackColor = true;
        environmentVariablesButton.Click += environmentVariablesButton_Click;
        // 
        // endpointLabel
        // 
        endpointLabel.AutoSize = true;
        endpointLabel.Location = new Point(230, 129);
        endpointLabel.Name = "endpointLabel";
        endpointLabel.Size = new Size(58, 15);
        endpointLabel.TabIndex = 18;
        endpointLabel.Text = "Endpoint:";
        // 
        // endpointTextBox
        // 
        endpointTextBox.Location = new Point(230, 147);
        endpointTextBox.Name = "endpointTextBox";
        endpointTextBox.Size = new Size(200, 23);
        endpointTextBox.TabIndex = 19;
        // 
        // transportModeLabel
        // 
        transportModeLabel.AutoSize = true;
        transportModeLabel.Location = new Point(230, 180);
        transportModeLabel.Name = "transportModeLabel";
        transportModeLabel.Size = new Size(94, 15);
        transportModeLabel.TabIndex = 20;
        transportModeLabel.Text = "Transport Mode:";
        // 
        // httpTransportModeComboBox
        // 
        httpTransportModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        httpTransportModeComboBox.FormattingEnabled = true;
        httpTransportModeComboBox.Location = new Point(230, 198);
        httpTransportModeComboBox.Name = "httpTransportModeComboBox";
        httpTransportModeComboBox.Size = new Size(200, 23);
        httpTransportModeComboBox.TabIndex = 21;
        // 
        // connectionTimeoutLabel
        // 
        connectionTimeoutLabel.AutoSize = true;
        connectionTimeoutLabel.Location = new Point(230, 231);
        connectionTimeoutLabel.Name = "connectionTimeoutLabel";
        connectionTimeoutLabel.Size = new Size(138, 15);
        connectionTimeoutLabel.TabIndex = 22;
        connectionTimeoutLabel.Text = "Connection Timeout (s):";
        // 
        // connectionTimeoutNumericUpDown
        // 
        connectionTimeoutNumericUpDown.Location = new Point(230, 249);
        connectionTimeoutNumericUpDown.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        connectionTimeoutNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        connectionTimeoutNumericUpDown.Name = "connectionTimeoutNumericUpDown";
        connectionTimeoutNumericUpDown.Size = new Size(200, 23);
        connectionTimeoutNumericUpDown.TabIndex = 23;
        connectionTimeoutNumericUpDown.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // additionalHeadersLabel
        // 
        additionalHeadersLabel.AutoSize = true;
        additionalHeadersLabel.Location = new Point(230, 282);
        additionalHeadersLabel.Name = "additionalHeadersLabel";
        additionalHeadersLabel.Size = new Size(109, 15);
        additionalHeadersLabel.TabIndex = 14;
        additionalHeadersLabel.Text = "Additional Headers:";
        // 
        // additionalHeadersButton
        // 
        additionalHeadersButton.Location = new Point(230, 300);
        additionalHeadersButton.Name = "additionalHeadersButton";
        additionalHeadersButton.Size = new Size(200, 23);
        additionalHeadersButton.TabIndex = 15;
        additionalHeadersButton.Text = "Edit...";
        additionalHeadersButton.UseVisualStyleBackColor = true;
        additionalHeadersButton.Click += additionalHeadersButton_Click;
        // 
        // okButton
        // 
        okButton.Enabled = false;
        okButton.Location = new Point(230, 340);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 23);
        okButton.TabIndex = 16;
        okButton.Text = "OK";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += okButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(311, 340);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 23);
        cancelButton.TabIndex = 17;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // serverListLabel
        // 
        serverListLabel.AutoSize = true;
        serverListLabel.Location = new Point(12, 9);
        serverListLabel.Name = "serverListLabel";
        serverListLabel.Size = new Size(76, 15);
        serverListLabel.TabIndex = 29;
        serverListLabel.Text = "MCP Servers:";
        // 
        // EditMcpServerForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(442, 375);
        Controls.Add(serverListLabel);
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
        Controls.Add(removeButton);
        Controls.Add(editButton);
        Controls.Add(addButton);
        Controls.Add(serverListBox);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "EditMcpServerForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Edit MCP Server";
        ((System.ComponentModel.ISupportInitialize)connectionTimeoutNumericUpDown).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ListBox serverListBox;
    private Button addButton;
    private Button editButton;
    private Button removeButton;
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
    private Label serverListLabel;
}