using System;
using System.Windows.Forms;
using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Models.Providers;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class ProviderSettingsForm : Form
{
    private readonly Provider _provider;
    private readonly bool _isBaseAddressEditable;
    private readonly bool _isApiKeyEditable;

    public string ApiKey => apiKeyTextBox.Text;
    public string BaseAddress => baseAddressTextBox.Text;

    public ProviderSettingsForm(Provider provider, string currentApiKey, string currentBaseAddress)
    {
        InitializeComponent();

        _provider = provider;
        _isApiKeyEditable = RibbonMain.IsApiKeyEditable(provider);
        _isBaseAddressEditable = RibbonMain.IsBaseAddressEditable(provider);


        this.Text = $"{provider} Settings";
        apiKeyTextBox.Text = _isApiKeyEditable ? currentApiKey : string.Empty;
        baseAddressTextBox.Text = currentBaseAddress;

        ConfigureControls();
    }

    private void ConfigureControls()
    {
        // API Key enabled state
        apiKeyTextBox.Enabled = _isApiKeyEditable;
        baseAddressTextBox.Enabled = _isBaseAddressEditable;
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        if (_isBaseAddressEditable && !string.IsNullOrWhiteSpace(baseAddressTextBox.Text))
        {
            if (!Uri.TryCreate(baseAddressTextBox.Text, UriKind.Absolute, out _))
            {
                MessageBox.Show("Please enter a valid Base Address (e.g., http://localhost:11434).", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Prevent closing
                return;
            }
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void ProviderSettingsForm_Load(object sender, EventArgs e)
    {

    }
}