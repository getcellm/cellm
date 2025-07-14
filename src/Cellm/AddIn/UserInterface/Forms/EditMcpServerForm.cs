using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class EditMcpServerForm : Form
{
    private readonly ILogger<EditMcpServerForm> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<ModelContextProtocolConfiguration> _mcpConfig;
    private readonly List<string> _existingStdioServers;
    private readonly List<string> _existingSseServers;
    private string? _editingServerName;
    private bool _isEditMode;
    private Dictionary<string, string?> _environmentVariables;
    private Dictionary<string, string> _headers;

    public EditMcpServerForm()
    {
        InitializeComponent();
        
        _logger = CellmAddIn.Services.GetRequiredService<ILogger<EditMcpServerForm>>();
        _configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();
        _mcpConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>();
        
        var currentConfig = _mcpConfig.CurrentValue;
        _existingStdioServers = currentConfig.StdioServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name!).ToList() ?? new List<string>();
        _existingSseServers = currentConfig.SseServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name!).ToList() ?? new List<string>();
        
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
        
        // Initialize empty collections
        _environmentVariables = new Dictionary<string, string?>();
        _headers = new Dictionary<string, string>();
        
        PopulateServerList();
        UpdateFieldsVisibility();
    }

    private void PopulateServerList()
    {
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

    private void transportTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateFieldsVisibility();
    }

    private void UpdateFieldsVisibility()
    {
        bool isStdio = transportTypeComboBox.SelectedIndex == 0;
        
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
        int baseHeight = 200;
        int fieldHeight = isStdio ? 200 : 180;
        this.Height = baseHeight + fieldHeight;
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
        // Not needed in edit form
    }

    private void editButton_Click(object sender, EventArgs e)
    {
        // Not needed - properties are always visible
    }

    private void removeButton_Click(object sender, EventArgs e)
    {
        if (serverListBox.SelectedItem == null) return;
        
        var selectedText = serverListBox.SelectedItem.ToString()!;
        bool isStdio = selectedText.StartsWith("[Standard I/O]");
        string serverName = selectedText.Substring(selectedText.IndexOf(']') + 2);
        
        var result = MessageBox.Show($"Are you sure you want to remove the MCP server '{serverName}'?", 
            "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            RemoveServer(serverName, isStdio);
            PopulateServerList();
        }
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;
        
        try
        {
            bool isStdio = transportTypeComboBox.SelectedIndex == 0;
            string serverName = nameTextBox.Text.Trim();
            
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
            
            if (_isEditMode && _editingServerName != null && _editingServerName != serverName)
            {
                // Remove old server first
                RemoveServer(_editingServerName, isStdio);
            }
            
            SaveServer(serverName, isStdio);
            PopulateServerList();
            CancelEdit();
            
            MessageBox.Show($"MCP server '{serverName}' saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            
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
        editButton.Enabled = true;
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
        
        var currentConfig = _mcpConfig.CurrentValue;
        
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
        string enabledConfigKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{serverName}";
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
        var currentConfig = _mcpConfig.CurrentValue;
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
        
        // Update configuration
        var configPath = "ModelContextProtocolConfiguration:StdioServers";
        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            RibbonMain.SetValue($"{configPath}:{i}:Name", server.Name);
            RibbonMain.SetValue($"{configPath}:{i}:Command", server.Command);
            
            if (server.Arguments != null && server.Arguments.Count > 0)
            {
                for (int j = 0; j < server.Arguments.Count; j++)
                {
                    RibbonMain.SetValue($"{configPath}:{i}:Arguments:{j}", server.Arguments[j]);
                }
            }
            
            if (server.WorkingDirectory != null)
            {
                RibbonMain.SetValue($"{configPath}:{i}:WorkingDirectory", server.WorkingDirectory);
            }
            
            if (server.EnvironmentVariables != null && server.EnvironmentVariables.Count > 0)
            {
                foreach (var kv in server.EnvironmentVariables)
                {
                    RibbonMain.SetValue($"{configPath}:{i}:EnvironmentVariables:{kv.Key}", kv.Value ?? "");
                }
            }
            
            // Only set ShutdownTimeout if it's not the default value
            if (server.ShutdownTimeout != TimeSpan.FromSeconds(5))
            {
                RibbonMain.SetValue($"{configPath}:{i}:ShutdownTimeout", server.ShutdownTimeout.ToString());
            }
        }
    }

    private void SaveSseServer(string serverName)
    {
        var currentConfig = _mcpConfig.CurrentValue;
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
        
        // Update configuration
        var configPath = "ModelContextProtocolConfiguration:SseServers";
        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            RibbonMain.SetValue($"{configPath}:{i}:Name", server.Name);
            RibbonMain.SetValue($"{configPath}:{i}:Endpoint", server.Endpoint.ToString());
            
            // Only set TransportMode if it's not the default value
            if (server.TransportMode != HttpTransportMode.AutoDetect)
            {
                RibbonMain.SetValue($"{configPath}:{i}:TransportMode", ((int)server.TransportMode).ToString());
            }
            
            // Only set ConnectionTimeout if it's not the default value
            if (server.ConnectionTimeout != TimeSpan.FromSeconds(30))
            {
                RibbonMain.SetValue($"{configPath}:{i}:ConnectionTimeout", server.ConnectionTimeout.ToString());
            }
            
            if (server.AdditionalHeaders != null && server.AdditionalHeaders.Count > 0)
            {
                foreach (var kv in server.AdditionalHeaders)
                {
                    RibbonMain.SetValue($"{configPath}:{i}:AdditionalHeaders:{kv.Key}", kv.Value);
                }
            }
        }
    }

    private void RemoveServer(string serverName, bool isStdio)
    {
        // Remove from enabled servers
        string enabledConfigKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{serverName}";
        RibbonMain.SetValue(enabledConfigKey, "");
        
        var currentConfig = _mcpConfig.CurrentValue;
        
        if (isStdio)
        {
            var servers = currentConfig.StdioServers?.Where(s => s.Name != serverName).ToList() ?? new List<StdioClientTransportOptions>();
            var configPath = "ModelContextProtocolConfiguration:StdioServers";
            
            // Clear existing configuration
            ClearConfigurationSection(configPath);
            
            // Re-add remaining servers
            for (int i = 0; i < servers.Count; i++)
            {
                var server = servers[i];
                RibbonMain.SetValue($"{configPath}:{i}:Name", server.Name);
                RibbonMain.SetValue($"{configPath}:{i}:Command", server.Command);
                
                if (server.Arguments != null && server.Arguments.Count > 0)
                {
                    for (int j = 0; j < server.Arguments.Count; j++)
                    {
                        RibbonMain.SetValue($"{configPath}:{i}:Arguments:{j}", server.Arguments[j]);
                    }
                }
                
                if (server.WorkingDirectory != null)
                {
                    RibbonMain.SetValue($"{configPath}:{i}:WorkingDirectory", server.WorkingDirectory);
                }
                
                if (server.EnvironmentVariables != null && server.EnvironmentVariables.Count > 0)
                {
                    foreach (var kv in server.EnvironmentVariables)
                    {
                        RibbonMain.SetValue($"{configPath}:{i}:EnvironmentVariables:{kv.Key}", kv.Value ?? "");
                    }
                }
                
                // Only set ShutdownTimeout if it's not the default value
                if (server.ShutdownTimeout != TimeSpan.FromSeconds(5))
                {
                    RibbonMain.SetValue($"{configPath}:{i}:ShutdownTimeout", server.ShutdownTimeout.ToString());
                }
            }
        }
        else
        {
            var servers = currentConfig.SseServers?.Where(s => s.Name != serverName).ToList() ?? new List<SseClientTransportOptions>();
            var configPath = "ModelContextProtocolConfiguration:SseServers";
            
            // Clear existing configuration
            ClearConfigurationSection(configPath);
            
            // Re-add remaining servers
            for (int i = 0; i < servers.Count; i++)
            {
                var server = servers[i];
                RibbonMain.SetValue($"{configPath}:{i}:Name", server.Name);
                RibbonMain.SetValue($"{configPath}:{i}:Endpoint", server.Endpoint.ToString());
                
                // Only set TransportMode if it's not the default value
                if (server.TransportMode != HttpTransportMode.AutoDetect)
                {
                    RibbonMain.SetValue($"{configPath}:{i}:TransportMode", ((int)server.TransportMode).ToString());
                }
                
                // Only set ConnectionTimeout if it's not the default value
                if (server.ConnectionTimeout != TimeSpan.FromSeconds(30))
                {
                    RibbonMain.SetValue($"{configPath}:{i}:ConnectionTimeout", server.ConnectionTimeout.ToString());
                }
                
                if (server.AdditionalHeaders != null && server.AdditionalHeaders.Count > 0)
                {
                    foreach (var kv in server.AdditionalHeaders)
                    {
                        RibbonMain.SetValue($"{configPath}:{i}:AdditionalHeaders:{kv.Key}", kv.Value);
                    }
                }
            }
        }
    }

    private void ClearConfigurationSection(string sectionPath)
    {
        if (_configuration is IConfigurationRoot configRoot)
        {
            var section = configRoot.GetSection(sectionPath);
            foreach (var child in section.GetChildren())
            {
                RibbonMain.SetValue(child.Path, "");
            }
        }
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

}