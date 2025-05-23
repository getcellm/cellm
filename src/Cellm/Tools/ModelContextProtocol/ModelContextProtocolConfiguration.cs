using ModelContextProtocol.Protocol.Transport;

namespace Cellm.Tools.ModelContextProtocol;

internal class ModelContextProtocolConfiguration
{
    public List<StdioClientTransportOptions> StdioServers { get; init; } =
    [
        new StdioClientTransportOptions
        {
            Command = "npx",
            Arguments = ["-y", "@playwright/mcp@latest"],
            Name = "Playwright"
        }
    ];

    public List<SseClientTransportOptions> SseServers { get; init; } = [];
}
