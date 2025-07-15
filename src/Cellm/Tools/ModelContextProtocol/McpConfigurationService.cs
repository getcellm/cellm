using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.AddIn.UserInterface.Ribbon;
using ExcelDna.Integration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Cellm.Tools.ModelContextProtocol;

public class McpConfigurationService : IMcpConfigurationService
{
    private readonly ILogger<McpConfigurationService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public McpConfigurationService(ILogger<McpConfigurationService> logger)
    {
        _logger = logger;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public IEnumerable<StdioClientTransportOptions> GetAllStdioServers()
    {
        var allServers = new List<StdioClientTransportOptions>();

        try
        {
            // Use RibbonHelpers to get merged configuration (base + local)
            var serversNode = RibbonMain.GetValueAsJsonNode("ModelContextProtocolConfiguration:StdioServers");
            if (serversNode is JsonArray serversArray)
            {
                foreach (var serverNode in serversArray)
                {
                    if (serverNode != null)
                    {
                        var server = JsonSerializer.Deserialize<StdioClientTransportOptions>(serverNode.ToString(), _jsonSerializerOptions);
                        if (server != null && !string.IsNullOrWhiteSpace(server.Name))
                        {
                            allServers.Add(server);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stdio servers");
        }

        return allServers;
    }

    public IEnumerable<SseClientTransportOptions> GetAllSseServers()
    {
        var allServers = new List<SseClientTransportOptions>();

        try
        {
            // Use RibbonHelpers to get merged configuration (base + local)
            var serversNode = RibbonMain.GetValueAsJsonNode("ModelContextProtocolConfiguration:SseServers");
            if (serversNode is JsonArray serversArray)
            {
                foreach (var serverNode in serversArray)
                {
                    if (serverNode != null)
                    {
                        var server = JsonSerializer.Deserialize<SseClientTransportOptions>(serverNode.ToString(), _jsonSerializerOptions);
                        if (server != null && !string.IsNullOrWhiteSpace(server.Name))
                        {
                            allServers.Add(server);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading SSE servers");
        }

        return allServers;
    }

    public IEnumerable<StdioClientTransportOptions> GetUserStdioServers()
    {
        var userServers = new List<StdioClientTransportOptions>();

        try
        {
            // Only get servers from local configuration
            var localServersNode = GetLocalConfigurationNode("ModelContextProtocolConfiguration:StdioServers");
            if (localServersNode is JsonArray serversArray)
            {
                foreach (var serverNode in serversArray)
                {
                    if (serverNode != null)
                    {
                        var server = JsonSerializer.Deserialize<StdioClientTransportOptions>(serverNode.ToString(), _jsonSerializerOptions);
                        if (server != null && !string.IsNullOrWhiteSpace(server.Name))
                        {
                            userServers.Add(server);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user stdio servers");
        }

        return userServers;
    }

    public IEnumerable<SseClientTransportOptions> GetUserSseServers()
    {
        var userServers = new List<SseClientTransportOptions>();

        try
        {
            // Only get servers from local configuration
            var localServersNode = GetLocalConfigurationNode("ModelContextProtocolConfiguration:SseServers");
            if (localServersNode is JsonArray serversArray)
            {
                foreach (var serverNode in serversArray)
                {
                    if (serverNode != null)
                    {
                        var server = JsonSerializer.Deserialize<SseClientTransportOptions>(serverNode.ToString(), _jsonSerializerOptions);
                        if (server != null && !string.IsNullOrWhiteSpace(server.Name))
                        {
                            userServers.Add(server);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user SSE servers");
        }

        return userServers;
    }

    public IEnumerable<string> GetAllServerNames()
    {
        var allNames = new List<string>();

        allNames.AddRange(GetAllStdioServers()
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name!));

        allNames.AddRange(GetAllSseServers()
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name!));

        return allNames.Distinct();
    }

    public StdioClientTransportOptions? GetStdioServer(string name)
    {
        return GetAllStdioServers().FirstOrDefault(s => s.Name == name);
    }

    public SseClientTransportOptions? GetSseServer(string name)
    {
        return GetAllSseServers().FirstOrDefault(s => s.Name == name);
    }

    public bool ServerExists(string name)
    {
        return GetAllServerNames().Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public void SaveUserServer(StdioClientTransportOptions server)
    {
        try
        {
            var userServers = GetUserStdioServers().ToList();

            // Remove existing server with same name
            userServers.RemoveAll(s => s.Name == server.Name);

            // Add the new server
            userServers.Add(server);

            // Save to local configuration
            SaveUserStdioServers(userServers);

            _logger.LogInformation("Saved stdio server: {ServerName}", server.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stdio server: {ServerName}", server.Name);
            throw;
        }
    }

    public void SaveUserServer(SseClientTransportOptions server)
    {
        try
        {
            var userServers = GetUserSseServers().ToList();

            // Remove existing server with same name
            userServers.RemoveAll(s => s.Name == server.Name);

            // Add the new server
            userServers.Add(server);

            // Save to local configuration
            SaveUserSseServers(userServers);

            _logger.LogInformation("Saved SSE server: {ServerName}", server.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SSE server: {ServerName}", server.Name);
            throw;
        }
    }

    public void RemoveUserServer(string name, bool isStdio)
    {
        try
        {
            if (isStdio)
            {
                var userServers = GetUserStdioServers().ToList();
                userServers.RemoveAll(s => s.Name == name);
                SaveUserStdioServers(userServers);
            }
            else
            {
                var userServers = GetUserSseServers().ToList();
                userServers.RemoveAll(s => s.Name == name);
                SaveUserSseServers(userServers);
            }

            _logger.LogInformation("Removed server: {ServerName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing server: {ServerName}", name);
            throw;
        }
    }

    public void SetServerEnabled(string name, bool enabled)
    {
        var configKey = $"CellmAddInConfiguration:EnableModelContextProtocolServers:{name}";

        if (enabled)
        {
            RibbonMain.SetValue(configKey, "true");
        }
        else
        {
            RibbonMain.RemoveKey(configKey);
        }

        _logger.LogInformation("Set server {ServerName} enabled: {Enabled}", name, enabled);
    }

    private JsonNode? GetLocalConfigurationNode(string key)
    {
        var keySegments = key.Split(':');
        var localFilePath = Path.Combine(
            ExcelDnaUtil.XllPathInfo?.Directory?.FullName ?? throw new InvalidOperationException("Cannot get configuration path"),
            "appsettings.Local.json");

        if (!File.Exists(localFilePath))
        {
            return null;
        }

        try
        {
            var localNode = JsonNode.Parse(File.ReadAllText(localFilePath));
            return GetValueFromNode(localNode, keySegments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading local configuration for key: {Key}", key);
            return null;
        }
    }

    private JsonNode? GetValueFromNode(JsonNode? node, string[] keySegments)
    {
        foreach (var segment in keySegments)
        {
            node = node is JsonObject obj
                && obj.TryGetPropertyValue(segment, out var childNode)
                ? childNode
                : null;

            if (node == null) break;
        }
        return node;
    }

    private void SaveUserStdioServers(List<StdioClientTransportOptions> servers)
    {
        var json = JsonSerializer.Serialize(servers, _jsonSerializerOptions);
        var jsonNode = JsonNode.Parse(json);
        RibbonMain.SetValue("ModelContextProtocolConfiguration:StdioServers", jsonNode!);
    }

    private void SaveUserSseServers(List<SseClientTransportOptions> servers)
    {
        var json = JsonSerializer.Serialize(servers, _jsonSerializerOptions);
        var jsonNode = JsonNode.Parse(json);
        RibbonMain.SetValue("ModelContextProtocolConfiguration:SseServers", jsonNode!);
    }
}