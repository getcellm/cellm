using ModelContextProtocol.Client;

namespace Cellm.Tools.ModelContextProtocol;

internal class ModelContextProtocolConfiguration
{
    public List<StdioClientTransportOptions> StdioServers { get; init; } = [];

    public List<SseClientTransportOptions> SseServers { get; init; } = [];
}
