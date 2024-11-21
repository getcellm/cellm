using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Llamafile;
using Cellm.Models.Local;
using Cellm.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Office.Interop.Excel;

namespace Cellm.Models.Ollama;

internal class OllamaRequestHandler : IModelRequestHandler<OllamaRequest, OllamaResponse>
{
    private record Ollama(Uri BaseAddress, Process Process);
    record Model(string Name);

    private readonly CellmConfiguration _cellmConfiguration;
    private readonly OllamaConfiguration _ollamaConfiguration;
    private readonly HttpClient _httpClient;
    private readonly LocalUtilities _localUtilities;
    private readonly ProcessManager _processManager;
    private readonly ILogger<OllamaRequestHandler> _logger;

    private readonly AsyncLazy<string> _ollamaExePath;
    private readonly AsyncLazy<Ollama> _ollama;

    public OllamaRequestHandler(
        IOptions<CellmConfiguration> cellmConfiguration,
        IOptions<OllamaConfiguration> ollamaConfiguration,
        HttpClient httpClient,
        LocalUtilities localUtilities,
        ProcessManager processManager,
        ILogger<OllamaRequestHandler> logger)
    {
        _cellmConfiguration = cellmConfiguration.Value;
        _ollamaConfiguration = ollamaConfiguration.Value;
        _httpClient = httpClient;
        _localUtilities = localUtilities;
        _processManager = processManager;
        _logger = logger;

        _ollamaExePath = new AsyncLazy<string>(async () =>
        {
            var zipFileName = string.Join("-", _ollamaConfiguration.OllamaUri.Segments.TakeLast(2));
            var zipFilePath = _localUtilities.CreateCellmFilePath(zipFileName);

            await _localUtilities.DownloadFile(_ollamaConfiguration.OllamaUri, zipFilePath);
            var ollamaPath = _localUtilities.ExtractFile(zipFilePath, _localUtilities.CreateCellmDirectory(nameof(Ollama), Path.GetFileNameWithoutExtension(zipFileName)));
            return Path.Combine(ollamaPath, "ollama.exe");
        });

        _ollama = new AsyncLazy<Ollama>(async () =>
        {
            var baseAddress = new UriBuilder("http", "localhost", _localUtilities.FindPort()).Uri;
            var process = await StartProcess(baseAddress);

            return new Ollama(baseAddress, process);
        });
    }

    public async Task<OllamaResponse> Handle(OllamaRequest request, CancellationToken cancellationToken)
    {
        // Start server on first call
        _ = await _ollama;

        var modelId = request.Prompt.Options.ModelId ?? _ollamaConfiguration.DefaultModel;

        const string path = "/v1/chat/completions";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        // Must instantiate manually because address can be set/changed only at instantiation
        var chatClient = await GetChatClient(address, modelId);
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OllamaResponse(prompt);
    }

    private async Task<Process> StartProcess(Uri baseAddress)
    {
        var processStartInfo = new ProcessStartInfo(await _ollamaExePath);

        processStartInfo.Arguments += $"serve ";
        processStartInfo.EnvironmentVariables.Add("OLLAMA_HOST", baseAddress.ToString());

        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardError = _cellmConfiguration.Debug;
        processStartInfo.RedirectStandardOutput = _cellmConfiguration.Debug;

        var process = Process.Start(processStartInfo) ?? throw new CellmException("Failed to run Ollama");

        if (_cellmConfiguration.Debug)
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogDebug(e.Data);
                    Debug.WriteLine(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        var address = new Uri(baseAddress, "/v1/models");
        await _localUtilities.WaitForServer(address, process);

        // Kill Ollama when Excel exits or dies
        _processManager.AssignProcessToExcel(process);

        return process;
    }

    private async Task<IChatClient> GetChatClient(Uri address, string modelId)
    {
        // Download model if it doesn't exist
        var models = await _httpClient.GetFromJsonAsync<List<Model>>("api/tags") ?? throw new CellmException();

        if (!models.Select(x => x.Name).Contains(modelId))
        {
            var body = new StringContent($"{{\"model\":\"{modelId}\", \"stream\": \"false\"}}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/pull", body);
            response.EnsureSuccessStatusCode();
        }

        return new ChatClientBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Use(new OllamaChatClient(address, modelId, _httpClient));
    }
}
