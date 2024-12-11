using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cellm.Models.Exceptions;
using Cellm.Models.Local.Utilities;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaRequestHandler : IModelRequestHandler<OllamaRequest, OllamaResponse>
{
    private record OllamaServer(Uri BaseAddress, Process Process);

    record Tags(List<Model> Models);
    record Model(string Name);
    record Progress(string Status);

    private readonly IChatClient _chatClient;
    private readonly OllamaConfiguration _ollamaConfiguration;
    private readonly HttpClient _httpClient;
    private readonly FileManager _fileManager;
    private readonly ProcessManager _processManager;
    private readonly ServerManager _serverManager;
    private readonly ILogger<OllamaRequestHandler> _logger;

    private readonly AsyncLazy<string> _ollamaExePath;
    private readonly AsyncLazy<OllamaServer> _ollamaServer;

    public OllamaRequestHandler(
        [FromKeyedServices(Provider.Ollama)] IChatClient chatClient,
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaConfiguration> ollamaConfiguration,
        FileManager fileManager,
        ProcessManager processManager,
        ServerManager serverManager,
        ILogger<OllamaRequestHandler> logger)
    {
        _chatClient = chatClient;
        _httpClient = httpClientFactory.CreateClient(nameof(Provider.Ollama));
        _ollamaConfiguration = ollamaConfiguration.Value;
        _fileManager = fileManager;
        _processManager = processManager;
        _serverManager = serverManager;
        _logger = logger;

        _ollamaExePath = new AsyncLazy<string>(async () =>
        {
            var zipFileName = string.Join("-", _ollamaConfiguration.ZipUrl.Segments.Select(x => x.Replace("/", string.Empty)).TakeLast(2));
            var zipFilePath = _fileManager.CreateCellmFilePath(zipFileName);

            await _fileManager.DownloadFileIfNotExists(
                _ollamaConfiguration.ZipUrl,
                zipFilePath);

            var ollamaPath = _fileManager.ExtractZipFileIfNotExtracted(
                zipFilePath,
                _fileManager.CreateCellmDirectory(nameof(Ollama), Path.GetFileNameWithoutExtension(zipFileName)));

            return Path.Combine(ollamaPath, "ollama.exe");
        });

        _ollamaServer = new AsyncLazy<OllamaServer>(async () =>
        {
            var ollamaExePath = await _ollamaExePath;
            var process = await StartProcess(ollamaExePath, _ollamaConfiguration.BaseAddress);

            return new OllamaServer(_ollamaConfiguration.BaseAddress, process);
        });
    }

    public async Task<OllamaResponse> Handle(OllamaRequest request, CancellationToken cancellationToken)
    {
        var serverIsRunning = await ServerIsRunning(_ollamaConfiguration.BaseAddress);
        if (_ollamaConfiguration.EnableServer && !serverIsRunning)
        {
            _ = await _ollamaServer;
        }

        var modelIsDownloaded = await ModelIsDownloaded(
            _ollamaConfiguration.BaseAddress,
            request.Prompt.Options.ModelId ?? _ollamaConfiguration.DefaultModel);

        if (!modelIsDownloaded)
        {
            await DownloadModel(
                _ollamaConfiguration.BaseAddress,
                request.Prompt.Options.ModelId ?? _ollamaConfiguration.DefaultModel);
        }

        var chatCompletion = await _chatClient.CompleteAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OllamaResponse(prompt);
    }

    private async Task<bool> ServerIsRunning(Uri baseAddress)
    {
        var response = await _httpClient.GetAsync(baseAddress);

        return response.IsSuccessStatusCode;
    }

    private async Task<bool> ModelIsDownloaded(Uri baseAddress, string modelId)
    {
        var tags = await _httpClient.GetFromJsonAsync<Tags>("api/tags") ?? throw new CellmModelException();

        return tags.Models.Select(x => x.Name).Contains(modelId);
    }

    private async Task DownloadModel(Uri baseAddress, string modelId)
    {
        try
        {
            var modelName = JsonSerializer.Serialize(new { name = modelId });
            var modelStringContent = new StringContent(modelName, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/pull", modelStringContent);

            response.EnsureSuccessStatusCode();

            var progress = await response.Content.ReadFromJsonAsync<List<Progress>>();

            if (progress is null || progress.Last().Status != "success")
            {
                throw new CellmModelException($"Ollama failed to download model {modelId}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new CellmModelException($"Ollama failed to download model {modelId} or {modelId} does not exist", ex);
        }
    }

    private async Task<Process> StartProcess(string ollamaExePath, Uri baseAddress)
    {
        var processStartInfo = new ProcessStartInfo(await _ollamaExePath);

        processStartInfo.ArgumentList.Add("serve");
        processStartInfo.EnvironmentVariables.Add("OLLAMA_HOST", baseAddress.ToString());

        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.RedirectStandardOutput = true;

        var process = Process.Start(processStartInfo) ?? throw new CellmModelException("Failed to run Ollama");

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

        var address = new Uri(baseAddress, "/v1/models");
        await _serverManager.WaitForServer(address, process);

        // Kill Ollama when Excel exits or dies
        _processManager.AssignProcessToExcel(process);

        return process;
    }
}
