using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace Cellm.Mcp;

internal class CellmClient
{
    private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _pipeName;

    public CellmClient()
        : this(GetPipeName())
    {
    }

    internal CellmClient(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName: _pipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        try
        {
            await pipe.ConnectAsync(_connectTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException("Cellm is not running in Excel.", ex);
        }

        using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
        {
            AutoFlush = true
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(request, _jsonOptions)).ConfigureAwait(false);

        var responseJson = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cellm closed the connection without returning a response.");

        return JsonSerializer.Deserialize<PromptResponse>(responseJson, _jsonOptions)
            ?? throw new InvalidOperationException("Cellm returned an invalid response.");
    }

    private static string GetPipeName()
    {
        var user = WindowsIdentity.GetCurrent().User?.Value
            ?? throw new InvalidOperationException("Unable to identify the current Windows user.");

        return $"Cellm-{user}";
    }
}
