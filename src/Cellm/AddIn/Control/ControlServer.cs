using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn.Control;

internal sealed class ControlServer(PromptRequestHandler promptRequestHandler, ILogger<ControlServer> logger) : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _serverTask;

    public void Start()
    {
        if (_serverTask is not null)
        {
            return;
        }

        promptRequestHandler.Start();
        _cancellationTokenSource = new CancellationTokenSource();
        _serverTask = RunAsync(_cancellationTokenSource.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                pipeName: GetPipeName(),
                direction: PipeDirection.InOut,
                maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                _ = HandleConnectionAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                break;
            }
            catch (Exception ex)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                logger.LogError(ex, "Cellm control server failed while accepting a connection");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        await using (pipe.ConfigureAwait(false))
        {
            try
            {
                using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                using var writer = new StreamWriter(pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
                {
                    AutoFlush = true
                };

                var requestJson = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The request was empty.");
                var request = JsonSerializer.Deserialize<PromptRequest>(requestJson, _jsonOptions)
                    ?? throw new InvalidOperationException("The request was invalid.");

                PromptResponse response;
                if (request.Version != 1)
                {
                    response = Error(request, $"Unsupported control protocol version {request.Version}.");
                }
                else if (!string.Equals(request.Method, "prompt", StringComparison.Ordinal))
                {
                    response = Error(request, $"Unsupported method '{request.Method}'.");
                }
                else
                {
                    response = await promptRequestHandler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
                }

                await writer.WriteLineAsync(JsonSerializer.Serialize(response, _jsonOptions)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, "Cellm MCP client disconnected");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cellm control request failed");
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        promptRequestHandler.Dispose();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _serverTask = null;
    }

    private static PromptResponse Error(PromptRequest request, string error)
    {
        return new PromptResponse(1, "error", request.Workbook, request.Worksheet, request.Cell, Error: error);
    }

    private static string GetPipeName()
    {
        var user = WindowsIdentity.GetCurrent().User?.Value
            ?? throw new InvalidOperationException("Unable to identify the current Windows user.");

        return $"Cellm-{user}";
    }
}
