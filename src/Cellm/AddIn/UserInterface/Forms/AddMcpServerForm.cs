using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class AddMcpServerForm : Form
{
    private readonly ILogger<AddMcpServerForm> _logger;
    private readonly IMcpConfigurationService _mcpConfigurationService;
    private Dictionary<string, string?> _environmentVariables = [];
    private Dictionary<string, string> _headers = [];

    public AddMcpServerForm()
    {
        InitializeComponent();

        _logger = CellmAddIn.Services.GetRequiredService<ILogger<AddMcpServerForm>>();
        _mcpConfigurationService = CellmAddIn.Services.GetRequiredService<IMcpConfigurationService>();

        InitializeForm();
    }

    private void InitializeForm()
    {
        transportTypeComboBox.Items.Add("Standard I/O");
        transportTypeComboBox.Items.Add("Streamable HTTP");
        transportTypeComboBox.SelectedIndex = 0;

        httpTransportModeComboBox.Items.Add("AutoDetect");
        httpTransportModeComboBox.Items.Add("SSE");
        httpTransportModeComboBox.Items.Add("Streamable HTTP");
        httpTransportModeComboBox.SelectedIndex = 0;

        connectionTimeoutNumericUpDown.Value = 30;

        UpdateFieldsVisibility();
    }

    private void transportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateFieldsVisibility();
    }

    private void UpdateFieldsVisibility()
    {
        var isStdio = transportTypeComboBox.SelectedIndex == 0;

        // Stdio fields
        commandLabel.Visible = isStdio;
        commandTextBox.Visible = isStdio;
        argumentsLabel.Visible = isStdio;
        argumentsTextBox.Visible = isStdio;
        workingDirectoryLabel.Visible = isStdio;
        workingDirectoryTextBox.Visible = isStdio;
        environmentVariablesLabel.Visible = isStdio;
        environmentVariablesButton.Visible = isStdio;

        // SSE fields
        endpointLabel.Visible = !isStdio;
        endpointTextBox.Visible = !isStdio;
        transportModeLabel.Visible = !isStdio;
        httpTransportModeComboBox.Visible = !isStdio;
        connectionTimeoutLabel.Visible = !isStdio;
        connectionTimeoutNumericUpDown.Visible = !isStdio;
        additionalHeadersLabel.Visible = !isStdio;
        additionalHeadersButton.Visible = !isStdio;
    }

    private void EnvironmentVariablesButton_Click(object sender, EventArgs e)
    {
        var form = new EnvironmentVariableEditorForm(_environmentVariables);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _environmentVariables = form.EnvironmentVariables;
        }
    }

    private void AdditionalHeadersButton_Click(object sender, EventArgs e)
    {
        var form = new HeadersEditorForm(_headers);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _headers = form.Headers;
        }
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            bool isStdio = transportTypeComboBox.SelectedIndex == 0;
            string serverName = nameTextBox.Text.Trim();

            // Check for duplicate names
            if (_mcpConfigurationService.ServerExists(serverName))
            {
                MessageBox.Show($"A server with the name '{serverName}' already exists.", "Duplicate Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prevent saving Playwright server (it's handled specially)
            if (serverName.Equals("Playwright", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The name 'Playwright' is reserved for the built-in MCP server.", "Reserved Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveServer(serverName, isStdio);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding MCP server");
            MessageBox.Show($"Error adding MCP server: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show("Please enter a server name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        bool isStdio = transportTypeComboBox.SelectedIndex == 0;

        if (isStdio)
        {
            if (string.IsNullOrWhiteSpace(commandTextBox.Text))
            {
                MessageBox.Show("Please enter a command.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(endpointTextBox.Text))
            {
                MessageBox.Show("Please enter an endpoint.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Uri.TryCreate(endpointTextBox.Text, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Please enter a valid HTTP or HTTPS endpoint.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }

    private void SaveServer(string serverName, bool isStdio)
    {
        // Enable the server
        _mcpConfigurationService.SetServerEnabled(serverName, true);

        if (isStdio)
        {
            SaveStdioServer(serverName);
        }
        else
        {
            SaveSseServer(serverName);
        }
    }

    private void SaveStdioServer(string serverName)
    {
        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(argumentsTextBox.Text))
        {
            arguments.AddRange(argumentsTextBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        var newServer = new StdioClientTransportOptions
        {
            Name = serverName,
            Command = commandTextBox.Text.Trim(),
            Arguments = arguments.Any() ? arguments : null,
            WorkingDirectory = !string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text) ? workingDirectoryTextBox.Text.Trim() : null,
            EnvironmentVariables = _environmentVariables.Any() ? _environmentVariables : null,
            ShutdownTimeout = TimeSpan.FromSeconds(5) // Default value
        };

        _mcpConfigurationService.SaveUserServer(newServer);
    }

    private void SaveSseServer(string serverName)
    {
        var newServer = new SseClientTransportOptions
        {
            Name = serverName,
            Endpoint = new Uri(endpointTextBox.Text.Trim()),
            TransportMode = (HttpTransportMode)httpTransportModeComboBox.SelectedIndex,
            ConnectionTimeout = TimeSpan.FromSeconds((double)connectionTimeoutNumericUpDown.Value),
            AdditionalHeaders = _headers.Any() ? _headers : null
        };

        _mcpConfigurationService.SaveUserServer(newServer);
    }

    private void AddMcpServerForm_Load(object sender, EventArgs e)
    {

    }
}