using System;
using System.Diagnostics;

// using System.Collections.Generic; // Not needed for this simple form
// using System.ComponentModel; // Not needed
// using System.Data; // Not needed
// using System.Drawing; // Might be needed if you handle graphics, but not here
// using System.Linq; // Not needed
using System.Windows.Forms;


namespace Cellm.AddIn;

public partial class LoginForm : Form
{
    public string Username => TextBoxUsername.Text;
    public string Password => TextBoxPassword.Text;

    public LoginForm()
    {
        InitializeComponent();
    }

    // Event Handler for the OK Button
    private void btnLogin_Click(object sender, EventArgs e)
    {
        // Basic validation: Ensure fields are not empty
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show("Please enter both username and password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None; // Prevent the form from closing
            TextBoxUsername.Focus();                   // Set focus back to the username field
            return;                                // Stop further execution
        }
        // If validation passes, the DialogResult remains OK (set by the button property),
        // and the form will close automatically when ShowDialog returns.
    }

    // Event Handler for the Cancel Button
    private void btnCancel_Click(object sender, EventArgs e)
    {
        // No-op
    }

    private void ForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        // Define the URL for password reset
        string passwordResetUrl = "https://dev.getcellm.com/password-reset"; // Use the correct URL

        try
        {
            // Use Process.Start to open the URL in the default browser
            ProcessStartInfo psi = new ProcessStartInfo
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
        string signUpUrl = "https://dev.getcellm.com/signup";

        try
        {
            // Use Process.Start to open the URL in the default browser
            ProcessStartInfo psi = new ProcessStartInfo
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
}
