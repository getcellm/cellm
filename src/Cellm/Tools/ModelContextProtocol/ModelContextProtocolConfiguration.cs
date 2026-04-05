using ModelContextProtocol.Client;

namespace Cellm.Tools.ModelContextProtocol;

internal class ModelContextProtocolConfiguration
{
    public List<StdioClientTransportOptions> StdioServers { get; init; } = [];

    public List<HttpClientTransportOptions> SseServers { get; init; } = [];
}
