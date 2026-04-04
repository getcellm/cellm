using Cellm.Tools.ModelContextProtocol;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.Tests.Integration.Helpers;

internal class TestMcpConfigurationService(IOptionsMonitor<ModelContextProtocolConfiguration> configuration) : IMcpConfigurationService
{
    public IEnumerable<StdioClientTransportOptions> GetAllStdioServers() =>
        configuration.CurrentValue.StdioServers;

    public IEnumerable<HttpClientTransportOptions> GetAllSseServers() =>
        configuration.CurrentValue.SseServers;

    public IEnumerable<string> GetAllServerNames() =>
        GetAllStdioServers().Select(s => s.Name!).Concat(GetAllSseServers().Select(s => s.Name!)).Distinct();

    public StdioClientTransportOptions? GetStdioServer(string name) =>
        GetAllStdioServers().FirstOrDefault(s => s.Name == name);

    public HttpClientTransportOptions? GetSseServer(string name) =>
        GetAllSseServers().FirstOrDefault(s => s.Name == name);

    public bool ServerExists(string name) =>
        GetAllServerNames().Contains(name, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<StdioClientTransportOptions> GetUserStdioServers() => [];
    public IEnumerable<HttpClientTransportOptions> GetUserSseServers() => [];
    public void SaveUserServer(StdioClientTransportOptions server) => throw new NotImplementedException();
    public void SaveUserServer(HttpClientTransportOptions server) => throw new NotImplementedException();
    public void RemoveUserServer(string name, bool isStdio) => throw new NotImplementedException();
    public void SetServerEnabled(string name, bool enabled) => throw new NotImplementedException();
}
