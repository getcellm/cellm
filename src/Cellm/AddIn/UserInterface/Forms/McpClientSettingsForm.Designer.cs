namespace Cellm.AddIn.UserInterface.Forms;

partial class McpClientSettingsForm
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
        workingDirectoryLabel = new Label();
        workingDirectoryTextBox = new TextBox();
        environmentVariablesLabel = new Label();
        environmentVariablesTextBox = new TextBox();
        shutdownTimeoutLabel = new Label();
        shutdownTimeoutNumericUpDown = new NumericUpDown();
        endpointLabel = new Label();
        endpointTextBox = new TextBox();
        transportModeLabel = new Label();
        httpTransportModeComboBox = new ComboBox();
        connectionTimeoutLabel = new Label();
        connectionTimeoutNumericUpDown = new NumericUpDown();
        additionalHeadersLabel = new Label();
        additionalHeadersTextBox = new TextBox();
        saveButton = new Button();
        cancelEditButton = new Button();
        closeButton = new Button();
        serverListLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)shutdownTimeoutNumericUpDown).BeginInit();
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
        // workingDirectoryLabel
        // 
        workingDirectoryLabel.AutoSize = true;
        workingDirectoryLabel.Location = new Point(230, 231);
        workingDirectoryLabel.Name = "workingDirectoryLabel";
        workingDirectoryLabel.Size = new Size(104, 15);
        workingDirectoryLabel.TabIndex = 12;
        workingDirectoryLabel.Text = "Working Directory:";
        // 
        // workingDirectoryTextBox
        // 
        workingDirectoryTextBox.Location = new Point(230, 249);
        workingDirectoryTextBox.Name = "workingDirectoryTextBox";
        workingDirectoryTextBox.Size = new Size(200, 23);
        workingDirectoryTextBox.TabIndex = 13;
        // 
        // environmentVariablesLabel
        // 
        environmentVariablesLabel.AutoSize = true;
        environmentVariablesLabel.Location = new Point(230, 282);
        environmentVariablesLabel.Name = "environmentVariablesLabel";
        environmentVariablesLabel.Size = new Size(139, 15);
        environmentVariablesLabel.TabIndex = 14;
        environmentVariablesLabel.Text = "Environment Variables:";
        // 
        // environmentVariablesTextBox
        // 
        environmentVariablesTextBox.Location = new Point(230, 300);
        environmentVariablesTextBox.Multiline = true;
        environmentVariablesTextBox.Name = "environmentVariablesTextBox";
        environmentVariablesTextBox.ScrollBars = ScrollBars.Vertical;
        environmentVariablesTextBox.Size = new Size(200, 60);
        environmentVariablesTextBox.TabIndex = 15;
        // 
        // shutdownTimeoutLabel
        // 
        shutdownTimeoutLabel.AutoSize = true;
        shutdownTimeoutLabel.Location = new Point(230, 372);
        shutdownTimeoutLabel.Name = "shutdownTimeoutLabel";
        shutdownTimeoutLabel.Size = new Size(133, 15);
        shutdownTimeoutLabel.TabIndex = 16;
        shutdownTimeoutLabel.Text = "Shutdown Timeout (s):";
        // 
        // shutdownTimeoutNumericUpDown
        // 
        shutdownTimeoutNumericUpDown.Location = new Point(230, 390);
        shutdownTimeoutNumericUpDown.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        shutdownTimeoutNumericUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        shutdownTimeoutNumericUpDown.Name = "shutdownTimeoutNumericUpDown";
        shutdownTimeoutNumericUpDown.Size = new Size(200, 23);
        shutdownTimeoutNumericUpDown.TabIndex = 17;
        shutdownTimeoutNumericUpDown.Value = new decimal(new int[] { 5, 0, 0, 0 });
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
        additionalHeadersLabel.TabIndex = 24;
        additionalHeadersLabel.Text = "Additional Headers:";
        // 
        // additionalHeadersTextBox
        // 
        additionalHeadersTextBox.Location = new Point(230, 300);
        additionalHeadersTextBox.Multiline = true;
        additionalHeadersTextBox.Name = "additionalHeadersTextBox";
        additionalHeadersTextBox.ScrollBars = ScrollBars.Vertical;
        additionalHeadersTextBox.Size = new Size(200, 60);
        additionalHeadersTextBox.TabIndex = 25;
        // 
        // saveButton
        // 
        saveButton.Enabled = false;
        saveButton.Location = new Point(230, 420);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(75, 23);
        saveButton.TabIndex = 26;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += saveButton_Click;
        // 
        // cancelEditButton
        // 
        cancelEditButton.Enabled = false;
        cancelEditButton.Location = new Point(311, 420);
        cancelEditButton.Name = "cancelEditButton";
        cancelEditButton.Size = new Size(75, 23);
        cancelEditButton.TabIndex = 27;
        cancelEditButton.Text = "Cancel";
        cancelEditButton.UseVisualStyleBackColor = true;
        cancelEditButton.Click += cancelEditButton_Click;
        // 
        // closeButton
        // 
        closeButton.Location = new Point(355, 460);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(75, 23);
        closeButton.TabIndex = 28;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += closeButton_Click;
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
        // McpClientSettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(442, 495);
        Controls.Add(serverListLabel);
        Controls.Add(closeButton);
        Controls.Add(cancelEditButton);
        Controls.Add(saveButton);
        Controls.Add(additionalHeadersTextBox);
        Controls.Add(additionalHeadersLabel);
        Controls.Add(connectionTimeoutNumericUpDown);
        Controls.Add(connectionTimeoutLabel);
        Controls.Add(httpTransportModeComboBox);
        Controls.Add(transportModeLabel);
        Controls.Add(endpointTextBox);
        Controls.Add(endpointLabel);
        Controls.Add(shutdownTimeoutNumericUpDown);
        Controls.Add(shutdownTimeoutLabel);
        Controls.Add(environmentVariablesTextBox);
        Controls.Add(environmentVariablesLabel);
        Controls.Add(workingDirectoryTextBox);
        Controls.Add(workingDirectoryLabel);
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
        Name = "McpClientSettingsForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "MCP Client Settings";
        ((System.ComponentModel.ISupportInitialize)shutdownTimeoutNumericUpDown).EndInit();
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
    private Label workingDirectoryLabel;
    private TextBox workingDirectoryTextBox;
    private Label environmentVariablesLabel;
    private TextBox environmentVariablesTextBox;
    private Label shutdownTimeoutLabel;
    private NumericUpDown shutdownTimeoutNumericUpDown;
    private Label endpointLabel;
    private TextBox endpointTextBox;
    private Label transportModeLabel;
    private ComboBox httpTransportModeComboBox;
    private Label connectionTimeoutLabel;
    private NumericUpDown connectionTimeoutNumericUpDown;
    private Label additionalHeadersLabel;
    private TextBox additionalHeadersTextBox;
    private Button saveButton;
    private Button cancelEditButton;
    private Button closeButton;
    private Label serverListLabel;
}