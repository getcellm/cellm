using System;
using System.Windows.Forms;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class HeaderEditForm : Form
{
    public string HeaderName => nameTextBox.Text.Trim();
    public string HeaderValue => valueTextBox.Text;

    public HeaderEditForm(string name, string value)
    {
        InitializeComponent();
        nameTextBox.Text = name;
        valueTextBox.Text = value;
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show("Please enter a header name.", "Validation Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}