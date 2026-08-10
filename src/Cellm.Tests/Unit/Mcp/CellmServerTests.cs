using System.IO;
using ModelContextProtocol.Client;
using Xunit;

namespace Cellm.Tests.Unit.Mcp;

public class CellmServerTests
{
    [Fact]
    public async Task Server_ExposesCellmPromptToolAsync()
    {
        var serverPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Cellm.Mcp",
            "bin",
            "Debug",
            "net9.0-windows",
            "Cellm.Mcp.dll"));

        Assert.True(File.Exists(serverPath), $"MCP server not found at {serverPath}");

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Cellm",
            Command = "dotnet",
            Arguments = [serverPath]
        });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: CancellationToken.None);
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);

        var tool = Assert.Single(tools);
        Assert.Equal("cellm_prompt", tool.Name);
    }
}
