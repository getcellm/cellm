using ModelContextProtocol.Protocol.Transport;

namespace Cellm.Tools.ModelContextProtocol;

internal class ModelContextProtocolConfiguration
{
    public List<StdioClientTransport> Servers { get; init; } = [];
}
