namespace Cellm.AddIn;

partial class LoginForm
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

    private void InitializeComponent()
    {
        LabelUsername = new Label();
        TextBoxUsername = new TextBox();
        LabelPassword = new Label();
        TextBoxPassword = new TextBox();
        btnLogin = new Button();
        btnCancel = new Button();
        lnkForgotPassword = new LinkLabel();
        SuspendLayout();

        // LabelUsername
        LabelUsername.AutoSize = true;
        LabelUsername.Location = new Point(12, 15);
        LabelUsername.Name = "LabelUsername";
        LabelUsername.Size = new Size(58, 13);
        LabelUsername.TabIndex = 0;
        LabelUsername.Text = "Username:";

        // TextBoxUsername
        TextBoxUsername.Location = new Point(76, 12);
        TextBoxUsername.Name = "TextBoxUsername";
        TextBoxUsername.Size = new Size(196, 20);
        TextBoxUsername.TabIndex = 1;

        // LabelPassword
        LabelPassword.AutoSize = true;
        LabelPassword.Location = new Point(12, 41);
        LabelPassword.Name = "LabelPassword";
        LabelPassword.Size = new Size(56, 13);
        LabelPassword.TabIndex = 2;
        LabelPassword.Text = "Password:";

        // TextBoxPassword
        TextBoxPassword.Location = new Point(76, 38);
        TextBoxPassword.Name = "TextBoxPassword";
        TextBoxPassword.Size = new Size(196, 20);
        TextBoxPassword.TabIndex = 3;
        TextBoxPassword.UseSystemPasswordChar = true;


        // --- ADDED: Initialize LinkLabel ---
        lnkForgotPassword.AutoSize = true; // Auto-size based on text
        lnkForgotPassword.Location = new Point(73, 61); // Position below password box, align left with textboxes
        lnkForgotPassword.Name = "lnkForgotPassword";
        lnkForgotPassword.Size = new Size(86, 13); // Approx size, AutoSize will adjust
        lnkForgotPassword.TabIndex = 4;           // Tab order after password box
        lnkForgotPassword.TabStop = true;         // Allow tabbing to it
        lnkForgotPassword.Text = "Forgot password?";

        // Wire up the LinkClicked event handler (defined in LoginForm.cs)
        lnkForgotPassword.LinkClicked += new LinkLabelLinkClickedEventHandler(ForgotPassword_LinkClicked);

        // btnLogin
        btnLogin.DialogResult = DialogResult.OK;
        btnLogin.Location = new Point(116, 88);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(75, 23);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "Login";
        btnLogin.UseVisualStyleBackColor = true;
        btnLogin.Click += new System.EventHandler(btnLogin_Click);

        // btnCancel
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(197, 88);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(75, 23);
        btnCancel.TabIndex = 6;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += new System.EventHandler(btnCancel_Click);

        // Form Settings
        AcceptButton = this.btnLogin;
        AutoScaleDimensions = new SizeF(6F, 13F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = this.btnCancel;
        ClientSize = new Size(284, 123);

        // Add Controls to Form
        Controls.Add(lnkForgotPassword);
        Controls.Add(btnCancel);
        Controls.Add(btnLogin);
        Controls.Add(TextBoxPassword);
        Controls.Add(LabelPassword);
        Controls.Add(TextBoxUsername);
        Controls.Add(LabelUsername);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Log in to Cellm";
        ResumeLayout(false);
        PerformLayout();
    }

    // Control Declarations
    private Label LabelUsername;
    private TextBox TextBoxUsername;
    private Label LabelPassword;
    private TextBox TextBoxPassword;
    private Button btnLogin;
    private Button btnCancel;
    // --- ADDED: LinkLabel Declaration ---
    private LinkLabel lnkForgotPassword;
}