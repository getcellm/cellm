﻿
namespace Cellm.AddIn.UserInterface.Forms;

partial class LoginForm
{
    private void InitializeComponent()
    {
        LabelUsername = new Label();
        TextBoxUsername = new TextBox();
        LabelPassword = new Label();
        TextBoxPassword = new TextBox();
        btnLogin = new Button();
        btnCancel = new Button();
        lnkForgotPassword = new LinkLabel();
        lnkCreateAccount = new LinkLabel();
        SuspendLayout();
        // 
        // LabelUsername
        // 
        LabelUsername.AutoSize = true;
        LabelUsername.Location = new Point(14, 17);
        LabelUsername.Margin = new Padding(4, 0, 4, 0);
        LabelUsername.Name = "LabelUsername";
        LabelUsername.Size = new Size(63, 15);
        LabelUsername.TabIndex = 0;
        LabelUsername.Text = "Username:";
        // 
        // TextBoxUsername
        // 
        TextBoxUsername.Location = new Point(89, 14);
        TextBoxUsername.Margin = new Padding(4, 3, 4, 3);
        TextBoxUsername.Name = "TextBoxUsername";
        TextBoxUsername.Size = new Size(228, 23);
        TextBoxUsername.TabIndex = 1;
        // 
        // LabelPassword
        // 
        LabelPassword.AutoSize = true;
        LabelPassword.Location = new Point(14, 47);
        LabelPassword.Margin = new Padding(4, 0, 4, 0);
        LabelPassword.Name = "LabelPassword";
        LabelPassword.Size = new Size(60, 15);
        LabelPassword.TabIndex = 2;
        LabelPassword.Text = "Password:";
        // 
        // TextBoxPassword
        // 
        TextBoxPassword.Location = new Point(89, 44);
        TextBoxPassword.Margin = new Padding(4, 3, 4, 3);
        TextBoxPassword.Name = "TextBoxPassword";
        TextBoxPassword.Size = new Size(228, 23);
        TextBoxPassword.TabIndex = 3;
        TextBoxPassword.UseSystemPasswordChar = true;
        // 
        // btnLogin
        // 
        btnLogin.DialogResult = DialogResult.OK;
        btnLogin.Location = new Point(135, 102);
        btnLogin.Margin = new Padding(4, 3, 4, 3);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(88, 27);
        btnLogin.TabIndex = 6;
        btnLogin.Text = "Login";
        btnLogin.UseVisualStyleBackColor = true;
        btnLogin.Click += BtnLogin_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(230, 102);
        btnCancel.Margin = new Padding(4, 3, 4, 3);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(88, 27);
        btnCancel.TabIndex = 7;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        // 
        // lnkForgotPassword
        // 
        lnkForgotPassword.AutoSize = true;
        lnkForgotPassword.Location = new Point(173, 70);
        lnkForgotPassword.Margin = new Padding(4, 0, 4, 0);
        lnkForgotPassword.Name = "lnkForgotPassword";
        lnkForgotPassword.Size = new Size(100, 15);
        lnkForgotPassword.TabIndex = 5;
        lnkForgotPassword.TabStop = true;
        lnkForgotPassword.Text = "Forgot password?";
        lnkForgotPassword.LinkClicked += ForgotPassword_LinkClicked;
        // 
        // lnkCreateAccount
        // 
        lnkCreateAccount.AutoSize = true;
        lnkCreateAccount.Location = new Point(85, 70);
        lnkCreateAccount.Margin = new Padding(4, 0, 4, 0);
        lnkCreateAccount.Name = "lnkCreateAccount";
        lnkCreateAccount.Size = new Size(89, 15);
        lnkCreateAccount.TabIndex = 4;
        lnkCreateAccount.TabStop = true;
        lnkCreateAccount.Text = "Create Account";
        lnkCreateAccount.LinkClicked += CreateAccount_LinkClicked;
        // 
        // LoginForm
        // 
        AcceptButton = btnLogin;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new Size(331, 142);
        Controls.Add(lnkForgotPassword);
        Controls.Add(lnkCreateAccount);
        Controls.Add(btnCancel);
        Controls.Add(btnLogin);
        Controls.Add(TextBoxPassword);
        Controls.Add(LabelPassword);
        Controls.Add(TextBoxUsername);
        Controls.Add(LabelUsername);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Log in to Cellm";
        Load += LoginForm_Load;
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
    private LinkLabel lnkForgotPassword;
    private LinkLabel lnkCreateAccount;
}