using ModelContextProtocol.Client;

namespace Cellm.Tools.ModelContextProtocol;

public interface IMcpConfigurationService
{
    /// <summary>
    /// Gets all servers (base + user) for display purposes
    /// </summary>
    IEnumerable<StdioClientTransportOptions> GetAllStdioServers();

    /// <summary>
    /// Gets all servers (base + user) for display purposes
    /// </summary>
    IEnumerable<SseClientTransportOptions> GetAllSseServers();

    /// <summary>
    /// Gets only user-defined servers from appsettings.Local.json
    /// </summary>
    IEnumerable<StdioClientTransportOptions> GetUserStdioServers();

    /// <summary>
    /// Gets only user-defined servers from appsettings.Local.json
    /// </summary>
    IEnumerable<SseClientTransportOptions> GetUserSseServers();

    /// <summary>
    /// Gets all server names (base + user) for validation
    /// </summary>
    IEnumerable<string> GetAllServerNames();

    /// <summary>
    /// Gets a specific server for editing
    /// </summary>
    StdioClientTransportOptions? GetStdioServer(string name);

    /// <summary>
    /// Gets a specific server for editing
    /// </summary>
    SseClientTransportOptions? GetSseServer(string name);

    /// <summary>
    /// Checks if a server name exists
    /// </summary>
    bool ServerExists(string name);

    /// <summary>
    /// Saves a user server to appsettings.Local.json
    /// </summary>
    void SaveUserServer(StdioClientTransportOptions server);

    /// <summary>
    /// Saves a user server to appsettings.Local.json
    /// </summary>
    void SaveUserServer(SseClientTransportOptions server);

    /// <summary>
    /// Removes a user server from appsettings.Local.json
    /// </summary>
    void RemoveUserServer(string name, bool isStdio);

    /// <summary>
    /// Enables/disables a server
    /// </summary>
    void SetServerEnabled(string name, bool enabled);
}