using System.Diagnostics;
using Cellm.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Cellm.AddIn.UserInterface.Forms;

public partial class LoginForm : Form
{
    public string Email => TextBoxEmail.Text;
    public string Password => TextBoxPassword.Text;

    public LoginForm()
    {
        InitializeComponent();
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        // Basic validation: Ensure fields are not empty
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show("Please enter both username and password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None; // Prevent the form from closing
            TextBoxEmail.Focus();               // Set focus back to the username field
            return;
        }

    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        // No-op
    }

    private void ForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();
        var passwordResetUrl = $"{accountConfiguration.CurrentValue.Homepage}/passwords/new";

        try
        {
            // Use Process.Start to open the URL in the default browser
            var psi = new ProcessStartInfo
            {
                FileName = passwordResetUrl,
                UseShellExecute = true // Important for opening URLs correctly
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log the error or show a message to the user
            Debug.WriteLine($"Error opening password reset link: {ex.Message}");
            MessageBox.Show($"Could not open the password reset link.\nPlease visit:\n{passwordResetUrl}\n\nError: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
        }
    }

    private void CreateAccount_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();
        var signUpUrl = $"{accountConfiguration.CurrentValue.Homepage}/user/new";

        try
        {
            // Use Process.Start to open the URL in the default browser
            var psi = new ProcessStartInfo
            {
                FileName = signUpUrl,
                UseShellExecute = true // Important for opening URLs correctly
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log the error or show a message to the user
            Debug.WriteLine($"Error opening create account link: {ex.Message}");
            MessageBox.Show($"Could not open the create account link.\nPlease visit:\n{signUpUrl}\n\nError: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
        }
    }

    private void LoginForm_Load(object sender, EventArgs e)
    {
        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();
        TextBoxEmail.Text = accountConfiguration.CurrentValue.Email;
    }
}
