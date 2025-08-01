namespace Cellm.AddIn.UserInterface.Forms;

partial class EnvironmentVariableEditorForm
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
        environmentVariablesListView = new ListView();
        nameColumnHeader = new ColumnHeader();
        valueColumnHeader = new ColumnHeader();
        newButton = new Button();
        editButton = new Button();
        removeButton = new Button();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();
        // 
        // environmentVariablesListView
        // 
        environmentVariablesListView.Columns.AddRange(new ColumnHeader[] { nameColumnHeader, valueColumnHeader });
        environmentVariablesListView.FullRowSelect = true;
        environmentVariablesListView.GridLines = true;
        environmentVariablesListView.HideSelection = false;
        environmentVariablesListView.Location = new Point(12, 12);
        environmentVariablesListView.MultiSelect = false;
        environmentVariablesListView.Name = "environmentVariablesListView";
        environmentVariablesListView.Size = new Size(400, 200);
        environmentVariablesListView.TabIndex = 0;
        environmentVariablesListView.UseCompatibleStateImageBehavior = false;
        environmentVariablesListView.View = View.Details;
        environmentVariablesListView.SelectedIndexChanged += environmentVariablesListView_SelectedIndexChanged;
        // 
        // nameColumnHeader
        // 
        nameColumnHeader.Text = "Name";
        nameColumnHeader.Width = 150;
        // 
        // valueColumnHeader
        // 
        valueColumnHeader.Text = "Value";
        valueColumnHeader.Width = 240;
        // 
        // newButton
        // 
        newButton.Location = new Point(12, 225);
        newButton.Name = "newButton";
        newButton.Size = new Size(75, 23);
        newButton.TabIndex = 1;
        newButton.Text = "New...";
        newButton.UseVisualStyleBackColor = true;
        newButton.Click += newButton_Click;
        // 
        // editButton
        // 
        editButton.Enabled = false;
        editButton.Location = new Point(93, 225);
        editButton.Name = "editButton";
        editButton.Size = new Size(75, 23);
        editButton.TabIndex = 2;
        editButton.Text = "Edit...";
        editButton.UseVisualStyleBackColor = true;
        editButton.Click += editButton_Click;
        // 
        // removeButton
        // 
        removeButton.Enabled = false;
        removeButton.Location = new Point(174, 225);
        removeButton.Name = "removeButton";
        removeButton.Size = new Size(75, 23);
        removeButton.TabIndex = 3;
        removeButton.Text = "Remove";
        removeButton.UseVisualStyleBackColor = true;
        removeButton.Click += removeButton_Click;
        // 
        // okButton
        // 
        okButton.Location = new Point(256, 265);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 23);
        okButton.TabIndex = 4;
        okButton.Text = "OK";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += okButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(337, 265);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 23);
        cancelButton.TabIndex = 5;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // EnvironmentVariableEditorForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(424, 300);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(removeButton);
        Controls.Add(editButton);
        Controls.Add(newButton);
        Controls.Add(environmentVariablesListView);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "EnvironmentVariableEditorForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Environment Variables";
        ResumeLayout(false);
    }

    #endregion

    private ListView environmentVariablesListView;
    private ColumnHeader nameColumnHeader;
    private ColumnHeader valueColumnHeader;
    private Button newButton;
    private Button editButton;
    private Button removeButton;
    private Button okButton;
    private Button cancelButton;
}