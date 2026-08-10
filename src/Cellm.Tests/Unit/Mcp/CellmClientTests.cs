using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Cellm.Mcp;
using Xunit;

namespace Cellm.Tests.Unit.Mcp;

public class CellmClientTests
{
    [Fact]
    public async Task PromptAsync_ExchangesJsonWithCellmAsync()
    {
        var pipeName = $"Cellm-Test-{Guid.NewGuid():N}";
        var serverTask = RunServerAsync(pipeName);
        var client = new CellmClient(pipeName);
        var request = new Cellm.Mcp.PromptRequest(
            1,
            "prompt",
            "Book1",
            "Sheet1",
            "B2",
            "What is 2+2?",
            ["A1"],
            null,
            false);

        var response = await client.PromptAsync(request, CancellationToken.None);
        await serverTask;

        Assert.Equal("completed", response.Status);
        Assert.Equal("4", response.Value?.ToString());
    }

    private static async Task RunServerAsync(string pipeName)
    {
        await using var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        await pipe.WaitForConnectionAsync(CancellationToken.None);

        using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        var requestJson = await reader.ReadLineAsync(CancellationToken.None);
        using var request = JsonDocument.Parse(requestJson!);

        Assert.Equal("prompt", request.RootElement.GetProperty("method").GetString());
        Assert.Equal("Book1", request.RootElement.GetProperty("workbook").GetString());
        Assert.Equal("B2", request.RootElement.GetProperty("cell").GetString());

        var response = new PromptResponse(1, "completed", "Book1", "Sheet1", "B2", "=PROMPT(\"What is 2+2?\")", "4");
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
