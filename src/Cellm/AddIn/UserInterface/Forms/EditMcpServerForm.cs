using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class EditMcpServerForm : Form
{
    private readonly ILogger<EditMcpServerForm> _logger = CellmAddIn.Services.GetRequiredService<ILogger<EditMcpServerForm>>();
    private readonly List<string> _existingStdioServers = [];
    private readonly List<string> _existingSseServers = [];
    private string? _editingServerName;
    private bool _isEditMode;
    private Dictionary<string, string?> _environmentVariables = [];
    private Dictionary<string, string> _headers = [];

    public EditMcpServerForm()
    {
        InitializeComponent();
        InitializeForm();
    }

    private void InitializeForm()
    {
        transportTypeComboBox.Items.Add("Standard I/O");
        transportTypeComboBox.Items.Add("Streamable HTTP");
        transportTypeComboBox.SelectedIndex = 0;

        httpTransportModeComboBox.Items.Add("AutoDetect");
        httpTransportModeComboBox.Items.Add("Server Sent Events (SSE)");
        httpTransportModeComboBox.Items.Add("Streamable HTTP");
        httpTransportModeComboBox.SelectedIndex = 0;

        connectionTimeoutNumericUpDown.Value = 30;

        // Load current server lists
        RefreshServerLists();
        PopulateServerList();
        UpdateFieldsVisibility();
    }

    private void RefreshServerLists()
    {
        var currentConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;

        var stdioServers = currentConfig.StdioServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.Name != "Playwright").Select(s => s.Name!).ToList() ?? new List<string>();
        var sseServers = currentConfig.SseServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.Name != "Playwright").Select(s => s.Name!).ToList() ?? new List<string>();

        _existingStdioServers.Clear();
        _existingStdioServers.AddRange(stdioServers);

        _existingSseServers.Clear();
        _existingSseServers.AddRange(sseServers);
    }

    private void PopulateServerList()
    {
        _logger.LogInformation($"PopulateServerList - Starting with {_existingStdioServers.Count} stdio and {_existingSseServers.Count} SSE servers");

        serverListBox.Items.Clear();

        foreach (var server in _existingStdioServers)
        {
            serverListBox.Items.Add($"[Standard I/O] {server}");
        }

        foreach (var server in _existingSseServers)
        {
            serverListBox.Items.Add($"[Streamable HTTP] {server}");
        }
    }

    private void TransportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
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

        // Adjust form height based on visible fields
        this.Height = 420;
    }

    private void environmentVariablesButton_Click(object sender, EventArgs e)
    {
        var form = new EnvironmentVariableEditorForm(_environmentVariables);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _environmentVariables = form.EnvironmentVariables;
        }
    }

    private void additionalHeadersButton_Click(object sender, EventArgs e)
    {
        var form = new HeadersEditorForm(_headers);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _headers = form.Headers;
        }
    }

    private void addButton_Click(object sender, EventArgs e)
    {
        var addForm = new AddMcpServerForm();
        if (addForm.ShowDialog() == DialogResult.OK)
        {
            // Allow some time for configuration file changes to be processed
            Thread.Sleep(500);

            // Refresh the server lists and repopulate the UI
            RefreshServerLists();
            PopulateServerList();
        }
    }


    private void RemoveButton_Click(object sender, EventArgs e)
    {
        if (serverListBox.SelectedItem == null) return;

        var selectedText = serverListBox.SelectedItem.ToString()!;
        var isStdio = selectedText.StartsWith("[Standard I/O]");
        var serverName = selectedText.Substring(selectedText.IndexOf(']') + 2);

        var result = MessageBox.Show($"Are you sure you want to remove the MCP server '{serverName}'?",
            "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _logger.LogInformation($"About to remove server: {serverName}, isStdio: {isStdio}");

            RemoveServer(serverName, isStdio);

            // Wait for file system changes to be processed
            Thread.Sleep(500);

            // Refresh the server lists and repopulate the UI
            RefreshServerLists();
            PopulateServerList();
            ClearFields();
            _isEditMode = false;
            _editingServerName = null;
            okButton.Enabled = false;

            // Update the ribbon UI
            RibbonMain._ribbonUi?.Invalidate();
        }
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            var isStdio = transportTypeComboBox.SelectedIndex == 0;
            var serverName = nameTextBox.Text.Trim();

            if (!_isEditMode)
            {
                // Check for duplicate names
                if ((_existingStdioServers.Contains(serverName) || _existingSseServers.Contains(serverName)))
                {
                    MessageBox.Show($"A server with the name '{serverName}' already exists.", "Duplicate Name",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Prevent saving Playwright server (it's handled specially)
            if (serverName.Equals("Playwright", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The name 'Playwright' is reserved for the built-in MCP server.", "Reserved Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isEditMode && _editingServerName != null && _editingServerName != serverName)
            {
                // Remove old server first
                RemoveServer(_editingServerName, isStdio);
            }

            SaveServer(serverName, isStdio);

            // Allow some time for configuration file changes to be processed
            System.Threading.Thread.Sleep(100);

            // Refresh the server lists and repopulate the UI
            RefreshServerLists();
            PopulateServerList();
            CancelEdit();


            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving MCP server");
            MessageBox.Show($"Error saving MCP server: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void CancelEdit()
    {
        _isEditMode = false;
        _editingServerName = null;
        ClearFields();
        addButton.Enabled = true;
        removeButton.Enabled = true;
        okButton.Enabled = false;
        cancelButton.Enabled = false;
        serverListBox.Enabled = true;
    }

    private void ClearFields()
    {
        nameTextBox.Clear();
        commandTextBox.Clear();
        argumentsTextBox.Clear();
        endpointTextBox.Clear();
        connectionTimeoutNumericUpDown.Value = 30;
        httpTransportModeComboBox.SelectedIndex = 0;
        _environmentVariables.Clear();
        _headers.Clear();
    }

    private void LoadServerData(string serverName, bool isStdio)
    {
        nameTextBox.Text = serverName;

        var currentConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;

        if (isStdio)
        {
            var server = currentConfig.StdioServers?.FirstOrDefault(s => s.Name == serverName);
            if (server != null)
            {
                commandTextBox.Text = server.Command;
                argumentsTextBox.Text = server.Arguments != null ? string.Join(" ", server.Arguments) : "";
                _environmentVariables = server.EnvironmentVariables != null ?
                    new Dictionary<string, string?>(server.EnvironmentVariables) : new Dictionary<string, string?>();
            }
        }
        else
        {
            var server = currentConfig.SseServers?.FirstOrDefault(s => s.Name == serverName);
            if (server != null)
            {
                endpointTextBox.Text = server.Endpoint.ToString();
                httpTransportModeComboBox.SelectedIndex = (int)server.TransportMode;
                connectionTimeoutNumericUpDown.Value = (decimal)server.ConnectionTimeout.TotalSeconds;
                _headers = server.AdditionalHeaders != null ?
                    new Dictionary<string, string>(server.AdditionalHeaders) : new Dictionary<string, string>();
            }
        }
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
        var enabledConfigKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{serverName}";
        RibbonMain.SetValue(enabledConfigKey, "true");

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
        var currentConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;
        var servers = currentConfig.StdioServers?.ToList() ?? new List<StdioClientTransportOptions>();

        // Remove existing server if editing
        servers.RemoveAll(s => s.Name == serverName);

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
            EnvironmentVariables = _environmentVariables.Any() ? _environmentVariables : null,
            ShutdownTimeout = TimeSpan.FromSeconds(5) // Default value
        };

        servers.Add(newServer);

        // Save the entire servers array as JSON
        SaveStdioServersConfiguration(servers);
    }

    private void SaveSseServer(string serverName)
    {
        var currentConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;
        var servers = currentConfig.SseServers?.ToList() ?? new List<SseClientTransportOptions>();

        // Remove existing server if editing
        servers.RemoveAll(s => s.Name == serverName);

        var newServer = new SseClientTransportOptions
        {
            Name = serverName,
            Endpoint = new Uri(endpointTextBox.Text.Trim()),
            TransportMode = (HttpTransportMode)httpTransportModeComboBox.SelectedIndex,
            ConnectionTimeout = TimeSpan.FromSeconds((double)connectionTimeoutNumericUpDown.Value),
            AdditionalHeaders = _headers.Any() ? _headers : null
        };

        servers.Add(newServer);

        // Save the entire servers array as JSON
        SaveSseServersConfiguration(servers);
    }

    private void RemoveServer(string serverName, bool isStdio)
    {
        try
        {
            _logger.LogInformation($"Removing {(isStdio ? "stdio" : "sse")} server: {serverName}");

            // Remove from enabled servers
            var enabledConfigKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{serverName}";
            RibbonMain.SetValue(enabledConfigKey, "");

            var currentConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;

            if (isStdio)
            {
                var servers = currentConfig.StdioServers?.Where(s => s.Name != serverName).ToList() ?? new List<StdioClientTransportOptions>();
                _logger.LogInformation($"Stdio servers before removal: {currentConfig.StdioServers?.Count ?? 0}, after removal: {servers.Count}");
                SaveStdioServersConfiguration(servers);
            }
            else
            {
                var servers = currentConfig.SseServers?.Where(s => s.Name != serverName).ToList() ?? new List<SseClientTransportOptions>();
                _logger.LogInformation($"SSE servers before removal: {currentConfig.SseServers?.Count ?? 0}, after removal: {servers.Count}");
                SaveSseServersConfiguration(servers);
            }

            _logger.LogInformation($"Successfully removed server: {serverName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing server {serverName}");
            throw;
        }
    }

    private void SaveStdioServersConfiguration(List<StdioClientTransportOptions> servers)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(servers, jsonOptions);
        var jsonNode = JsonNode.Parse(json);
        RibbonMain.SetValue("ModelContextProtocolConfiguration:StdioServers", jsonNode!);
    }

    private void SaveSseServersConfiguration(List<SseClientTransportOptions> servers)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(servers, jsonOptions);
        var jsonNode = JsonNode.Parse(json);
        RibbonMain.SetValue("ModelContextProtocolConfiguration:SseServers", jsonNode!);
    }

    private void serverListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        bool hasSelection = serverListBox.SelectedItem != null;
        removeButton.Enabled = hasSelection;
        okButton.Enabled = hasSelection;

        if (hasSelection)
        {
            var selectedText = serverListBox.SelectedItem?.ToString()!;
            bool isStdio = selectedText.StartsWith("[Standard I/O]");
            string serverName = selectedText.Substring(selectedText.IndexOf(']') + 2);
            _editingServerName = serverName;
            _isEditMode = true;

            transportTypeComboBox.SelectedIndex = isStdio ? 0 : 1;
            LoadServerData(serverName, isStdio);
        }
        else
        {
            ClearFields();
            _isEditMode = false;
            _editingServerName = null;
        }
    }

    private void EditMcpServerForm_Load(object sender, EventArgs e)
    {

    }

    private void EditMcpServerForm_Load_1(object sender, EventArgs e)
    {

    }
}