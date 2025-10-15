using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class EditMcpServerForm : Form
{
    private readonly ILogger<EditMcpServerForm> _logger;
    private readonly IMcpConfigurationService _mcpConfigurationService;
    private string? _editingServerName;
    private bool _isEditMode;
    private Dictionary<string, string?> _environmentVariables = [];
    private Dictionary<string, string> _headers = [];

    public EditMcpServerForm()
    {
        _logger = CellmAddIn.Services.GetRequiredService<ILogger<EditMcpServerForm>>();
        _mcpConfigurationService = CellmAddIn.Services.GetRequiredService<IMcpConfigurationService>();

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
        PopulateServerList();
    }

    private void PopulateServerList()
    {
        serverListView.Items.Clear();

        // Get all servers from the service (excludes Playwright automatically)
        var stdioServers = _mcpConfigurationService.GetAllStdioServers()
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.Name != "Playwright")
            .ToList();

        var sseServers = _mcpConfigurationService.GetAllSseServers()
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.Name != "Playwright")
            .ToList();

        _logger.LogInformation("PopulateServerList - Found {stdioCount} stdio and {sseCount} SSE servers", stdioServers.Count, sseServers.Count);

        foreach (var server in stdioServers)
        {
            var item = new ListViewItem("Standard I/O");
            item.SubItems.Add(server.Name!);
            item.Tag = new { Name = server.Name, IsStdio = true };
            serverListView.Items.Add(item);
        }

        foreach (var server in sseServers)
        {
            var item = new ListViewItem("Streamable HTTP");
            item.SubItems.Add(server.Name!);
            item.Tag = new { Name = server.Name, IsStdio = false };
            serverListView.Items.Add(item);
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

    private void AddButton_Click(object sender, EventArgs e)
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
        if (serverListView.SelectedItems.Count == 0) return;

        var selectedItem = serverListView.SelectedItems[0];
        var serverInfo = (dynamic?)selectedItem.Tag;
        var serverName = serverInfo?.Name;
        var isStdio = serverInfo?.IsStdio ?? throw new NullReferenceException(nameof(serverInfo));

        var result = MessageBox.Show($"Are you sure you want to remove the MCP server '{serverName}'?",
            "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _logger.LogInformation("About to remove server: {serverName}, isStdio: {isStdio}", (string)serverName, (bool)isStdio);

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

    private void OkButton_Click(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            var isStdio = transportTypeComboBox.SelectedIndex == 0;
            var serverName = nameTextBox.Text.Trim();

            if (!_isEditMode)
            {
                // Check for duplicate names
                if (_mcpConfigurationService.ServerExists(serverName))
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
            Thread.Sleep(500);

            // Refresh the server lists and repopulate the UI
            RefreshServerLists();
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

    private void CancelButton_Click(object sender, EventArgs e)
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
        serverListView.Enabled = true;
    }

    private void ClearFields()
    {
        nameTextBox.Clear();
        commandTextBox.Clear();
        argumentsTextBox.Clear();
        workingDirectoryTextBox.Clear();
        endpointTextBox.Clear();
        connectionTimeoutNumericUpDown.Value = 30;
        httpTransportModeComboBox.SelectedIndex = 0;
        _environmentVariables.Clear();
        _headers.Clear();
    }

    private void LoadServerData(string serverName, bool isStdio)
    {
        nameTextBox.Text = serverName;

        if (isStdio)
        {
            var server = _mcpConfigurationService.GetStdioServer(serverName);
            if (server != null)
            {
                commandTextBox.Text = server.Command;
                argumentsTextBox.Text = server.Arguments != null ? string.Join(" ", server.Arguments) : "";
                workingDirectoryTextBox.Text = server.WorkingDirectory ?? "";
                _environmentVariables = server.EnvironmentVariables != null ?
                    new Dictionary<string, string?>(server.EnvironmentVariables) : [];
            }
        }
        else
        {
            var server = _mcpConfigurationService.GetSseServer(serverName);

            if (server != null)
            {
                endpointTextBox.Text = server.Endpoint.ToString();
                httpTransportModeComboBox.SelectedIndex = (int)server.TransportMode;
                connectionTimeoutNumericUpDown.Value = (decimal)server.ConnectionTimeout.TotalSeconds;
                _headers = server.AdditionalHeaders != null ?
                    new Dictionary<string, string>(server.AdditionalHeaders) : [];
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
            Arguments = arguments.Count > 0 ? arguments : null,
            WorkingDirectory = !string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text) ? workingDirectoryTextBox.Text.Trim() : null,
            EnvironmentVariables = _environmentVariables.Count > 0 ? _environmentVariables : null,
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
            AdditionalHeaders = _headers.Count > 0 ? _headers : null
        };

        _mcpConfigurationService.SaveUserServer(newServer);
    }

    private void RemoveServer(string serverName, bool isStdio)
    {
        try
        {
            // Remove from enabled servers
            _mcpConfigurationService.SetServerEnabled(serverName, false);

            // Remove from user servers
            _mcpConfigurationService.RemoveUserServer(serverName, isStdio);

            _logger.LogInformation("Removed server: {serverName}", serverName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing server {serverName}", serverName);
            throw;
        }
    }

    private void ServerListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        var hasSelection = serverListView.SelectedItems.Count > 0;

        removeButton.Enabled = hasSelection;
        okButton.Enabled = hasSelection;

        if (hasSelection)
        {
            var selectedItem = serverListView.SelectedItems[0];
            var serverInfo = (dynamic?)selectedItem.Tag;
            var serverName = serverInfo?.Name;
            var isStdio = serverInfo?.IsStdio ?? throw new NullReferenceException(nameof(serverInfo));

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
}