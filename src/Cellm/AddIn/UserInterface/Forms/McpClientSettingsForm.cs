using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.AddIn.UserInterface.Forms;

public partial class McpClientSettingsForm : Form
{
    private readonly ILogger<McpClientSettingsForm> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<ModelContextProtocolConfiguration> _mcpConfig;
    private readonly List<string> _existingStdioServers;
    private readonly List<string> _existingSseServers;
    private string? _editingServerName;
    private bool _isEditMode;

    public McpClientSettingsForm()
    {
        InitializeComponent();
        
        _logger = CellmAddIn.Services.GetRequiredService<ILogger<McpClientSettingsForm>>();
        _configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();
        _mcpConfig = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>();
        
        var currentConfig = _mcpConfig.CurrentValue;
        _existingStdioServers = currentConfig.StdioServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name!).ToList() ?? new List<string>();
        _existingSseServers = currentConfig.SseServers?.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name!).ToList() ?? new List<string>();
        
        InitializeForm();
    }

    private void InitializeForm()
    {
        transportTypeComboBox.Items.Add("Stdio");
        transportTypeComboBox.Items.Add("SSE");
        transportTypeComboBox.SelectedIndex = 0;
        
        httpTransportModeComboBox.Items.Add("AutoDetect");
        httpTransportModeComboBox.Items.Add("Sse");
        httpTransportModeComboBox.Items.Add("StreamableHttp");
        httpTransportModeComboBox.SelectedIndex = 0;
        
        shutdownTimeoutNumericUpDown.Value = 5;
        connectionTimeoutNumericUpDown.Value = 30;
        
        PopulateServerList();
        UpdateFieldsVisibility();
    }

    private void PopulateServerList()
    {
        serverListBox.Items.Clear();
        
        foreach (var server in _existingStdioServers)
        {
            serverListBox.Items.Add($"[Stdio] {server}");
        }
        
        foreach (var server in _existingSseServers)
        {
            serverListBox.Items.Add($"[SSE] {server}");
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
        workingDirectoryLabel.Visible = isStdio;
        workingDirectoryTextBox.Visible = isStdio;
        environmentVariablesLabel.Visible = isStdio;
        environmentVariablesTextBox.Visible = isStdio;
        shutdownTimeoutLabel.Visible = isStdio;
        shutdownTimeoutNumericUpDown.Visible = isStdio;
        
        // SSE fields
        endpointLabel.Visible = !isStdio;
        endpointTextBox.Visible = !isStdio;
        transportModeLabel.Visible = !isStdio;
        httpTransportModeComboBox.Visible = !isStdio;
        connectionTimeoutLabel.Visible = !isStdio;
        connectionTimeoutNumericUpDown.Visible = !isStdio;
        additionalHeadersLabel.Visible = !isStdio;
        additionalHeadersTextBox.Visible = !isStdio;
        
        // Adjust form height based on visible fields
        int baseHeight = 200;
        int fieldHeight = isStdio ? 250 : 200;
        this.Height = baseHeight + fieldHeight;
    }

    private void addButton_Click(object sender, EventArgs e)
    {
        _isEditMode = false;
        _editingServerName = null;
        ClearFields();
        addButton.Enabled = false;
        editButton.Enabled = false;
        removeButton.Enabled = false;
        saveButton.Enabled = true;
        cancelEditButton.Enabled = true;
        serverListBox.Enabled = false;
    }

    private void editButton_Click(object sender, EventArgs e)
    {
        if (serverListBox.SelectedItem == null) return;
        
        _isEditMode = true;
        var selectedText = serverListBox.SelectedItem.ToString()!;
        bool isStdio = selectedText.StartsWith("[Stdio]");
        string serverName = selectedText.Substring(selectedText.IndexOf(']') + 2);
        _editingServerName = serverName;
        
        transportTypeComboBox.SelectedIndex = isStdio ? 0 : 1;
        LoadServerData(serverName, isStdio);
        
        addButton.Enabled = false;
        editButton.Enabled = false;
        removeButton.Enabled = false;
        saveButton.Enabled = true;
        cancelEditButton.Enabled = true;
        serverListBox.Enabled = false;
    }

    private void removeButton_Click(object sender, EventArgs e)
    {
        if (serverListBox.SelectedItem == null) return;
        
        var selectedText = serverListBox.SelectedItem.ToString()!;
        bool isStdio = selectedText.StartsWith("[Stdio]");
        string serverName = selectedText.Substring(selectedText.IndexOf(']') + 2);
        
        var result = MessageBox.Show($"Are you sure you want to remove the MCP server '{serverName}'?", 
            "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            RemoveServer(serverName, isStdio);
            PopulateServerList();
        }
    }

    private void saveButton_Click(object sender, EventArgs e)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving MCP server");
            MessageBox.Show($"Error saving MCP server: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void cancelEditButton_Click(object sender, EventArgs e)
    {
        CancelEdit();
    }

    private void CancelEdit()
    {
        _isEditMode = false;
        _editingServerName = null;
        ClearFields();
        addButton.Enabled = true;
        editButton.Enabled = true;
        removeButton.Enabled = true;
        saveButton.Enabled = false;
        cancelEditButton.Enabled = false;
        serverListBox.Enabled = true;
    }

    private void ClearFields()
    {
        nameTextBox.Clear();
        commandTextBox.Clear();
        argumentsTextBox.Clear();
        workingDirectoryTextBox.Clear();
        environmentVariablesTextBox.Clear();
        endpointTextBox.Clear();
        additionalHeadersTextBox.Clear();
        shutdownTimeoutNumericUpDown.Value = 5;
        connectionTimeoutNumericUpDown.Value = 30;
        httpTransportModeComboBox.SelectedIndex = 0;
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
                workingDirectoryTextBox.Text = server.WorkingDirectory ?? "";
                environmentVariablesTextBox.Text = server.EnvironmentVariables != null ? 
                    string.Join("\r\n", server.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}")) : "";
                shutdownTimeoutNumericUpDown.Value = (decimal)server.ShutdownTimeout.TotalSeconds;
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
                additionalHeadersTextBox.Text = server.AdditionalHeaders != null ? 
                    string.Join("\r\n", server.AdditionalHeaders.Select(kv => $"{kv.Key}={kv.Value}")) : "";
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
        _configuration[enabledConfigKey] = "true";
        
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
        
        var environmentVariables = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(environmentVariablesTextBox.Text))
        {
            foreach (var line in environmentVariablesTextBox.Text.Split('\r', '\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    environmentVariables[parts[0]] = parts[1];
                }
            }
        }
        
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
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectoryTextBox.Text) ? null : workingDirectoryTextBox.Text.Trim(),
            EnvironmentVariables = environmentVariables.Any() ? environmentVariables : null,
            ShutdownTimeout = TimeSpan.FromSeconds((double)shutdownTimeoutNumericUpDown.Value)
        };
        
        servers.Add(newServer);
        
        // Update configuration
        var configPath = "ModelContextProtocolConfiguration:StdioServers";
        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            _configuration[$"{configPath}:{i}:Name"] = server.Name;
            _configuration[$"{configPath}:{i}:Command"] = server.Command;
            _configuration[$"{configPath}:{i}:WorkingDirectory"] = server.WorkingDirectory;
            _configuration[$"{configPath}:{i}:ShutdownTimeout"] = server.ShutdownTimeout.ToString();
            
            if (server.Arguments != null)
            {
                for (int j = 0; j < server.Arguments.Count; j++)
                {
                    _configuration[$"{configPath}:{i}:Arguments:{j}"] = server.Arguments[j];
                }
            }
            
            if (server.EnvironmentVariables != null)
            {
                foreach (var kv in server.EnvironmentVariables)
                {
                    _configuration[$"{configPath}:{i}:EnvironmentVariables:{kv.Key}"] = kv.Value;
                }
            }
        }
    }

    private void SaveSseServer(string serverName)
    {
        var currentConfig = _mcpConfig.CurrentValue;
        var servers = currentConfig.SseServers?.ToList() ?? new List<SseClientTransportOptions>();
        
        // Remove existing server if editing
        servers.RemoveAll(s => s.Name == serverName);
        
        var additionalHeaders = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(additionalHeadersTextBox.Text))
        {
            foreach (var line in additionalHeadersTextBox.Text.Split('\r', '\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    additionalHeaders[parts[0]] = parts[1];
                }
            }
        }
        
        var newServer = new SseClientTransportOptions
        {
            Name = serverName,
            Endpoint = new Uri(endpointTextBox.Text.Trim()),
            TransportMode = (HttpTransportMode)httpTransportModeComboBox.SelectedIndex,
            ConnectionTimeout = TimeSpan.FromSeconds((double)connectionTimeoutNumericUpDown.Value),
            AdditionalHeaders = additionalHeaders.Any() ? additionalHeaders : null
        };
        
        servers.Add(newServer);
        
        // Update configuration
        var configPath = "ModelContextProtocolConfiguration:SseServers";
        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            _configuration[$"{configPath}:{i}:Name"] = server.Name;
            _configuration[$"{configPath}:{i}:Endpoint"] = server.Endpoint.ToString();
            _configuration[$"{configPath}:{i}:TransportMode"] = ((int)server.TransportMode).ToString();
            _configuration[$"{configPath}:{i}:ConnectionTimeout"] = server.ConnectionTimeout.ToString();
            
            if (server.AdditionalHeaders != null)
            {
                foreach (var kv in server.AdditionalHeaders)
                {
                    _configuration[$"{configPath}:{i}:AdditionalHeaders:{kv.Key}"] = kv.Value;
                }
            }
        }
    }

    private void RemoveServer(string serverName, bool isStdio)
    {
        // Remove from enabled servers
        string enabledConfigKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{serverName}";
        _configuration[enabledConfigKey] = null;
        
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
                _configuration[$"{configPath}:{i}:Name"] = server.Name;
                _configuration[$"{configPath}:{i}:Command"] = server.Command;
                _configuration[$"{configPath}:{i}:WorkingDirectory"] = server.WorkingDirectory;
                _configuration[$"{configPath}:{i}:ShutdownTimeout"] = server.ShutdownTimeout.ToString();
                
                if (server.Arguments != null)
                {
                    for (int j = 0; j < server.Arguments.Count; j++)
                    {
                        _configuration[$"{configPath}:{i}:Arguments:{j}"] = server.Arguments[j];
                    }
                }
                
                if (server.EnvironmentVariables != null)
                {
                    foreach (var kv in server.EnvironmentVariables)
                    {
                        _configuration[$"{configPath}:{i}:EnvironmentVariables:{kv.Key}"] = kv.Value;
                    }
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
                _configuration[$"{configPath}:{i}:Name"] = server.Name;
                _configuration[$"{configPath}:{i}:Endpoint"] = server.Endpoint.ToString();
                _configuration[$"{configPath}:{i}:TransportMode"] = ((int)server.TransportMode).ToString();
                _configuration[$"{configPath}:{i}:ConnectionTimeout"] = server.ConnectionTimeout.ToString();
                
                if (server.AdditionalHeaders != null)
                {
                    foreach (var kv in server.AdditionalHeaders)
                    {
                        _configuration[$"{configPath}:{i}:AdditionalHeaders:{kv.Key}"] = kv.Value;
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
                _configuration[child.Path] = null;
            }
        }
    }

    private void serverListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        bool hasSelection = serverListBox.SelectedItem != null;
        editButton.Enabled = hasSelection;
        removeButton.Enabled = hasSelection;
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }
}