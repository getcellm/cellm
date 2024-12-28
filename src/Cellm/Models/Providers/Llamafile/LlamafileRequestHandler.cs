using System.Diagnostics;
using Cellm.Models.Exceptions;
using Cellm.Models.Local.Utilities;
using Cellm.Models.Providers.OpenAiCompatible;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Llamafile;

internal class LlamafileRequestHandler : IProviderRequestHandler<LlamafileRequest, LlamafileResponse>
{
    private record Llamafile(string ModelPath, Uri BaseAddress, Process Process);

    private readonly AsyncLazy<string> _llamafileExePath;
    private readonly Dictionary<string, AsyncLazy<Llamafile>> _llamafiles;
    private readonly ProcessManager _processManager;
    private readonly FileManager _fileManager;
    private readonly ServerManager _serverManager;

    private readonly LlamafileConfiguration _llamafileConfiguration;

    private readonly ISender _sender;
    private readonly ILogger<LlamafileRequestHandler> _logger;

    public LlamafileRequestHandler(
        IOptions<LlamafileConfiguration> llamafileConfiguration,
        ISender sender,
        HttpClient httpClient,
        FileManager fileManager,
        ProcessManager processManager,
        ServerManager serverManager,
        ILogger<LlamafileRequestHandler> logger)
    {
        _llamafileConfiguration = llamafileConfiguration.Value;
        _sender = sender;
        _fileManager = fileManager;
        _processManager = processManager;
        _serverManager = serverManager;
        _logger = logger;

        _llamafileExePath = new AsyncLazy<string>(async () =>
        {
            var llamafileName = Path.GetFileName(_llamafileConfiguration.LlamafileUrl.Segments.Last());
            return await _fileManager.DownloadFileIfNotExists(_llamafileConfiguration.LlamafileUrl, _fileManager.CreateCellmFilePath(CreateModelFileName($"{llamafileName}.exe"), "Llamafile"));
        });

        _llamafiles = _llamafileConfiguration.Models.ToDictionary(x => x.Key, x => new AsyncLazy<Llamafile>(async () =>
        {
            // Download Llamafile
            var exePath = await _llamafileExePath;

            // Download model
            var modelPath = await _fileManager.DownloadFileIfNotExists(x.Value, _fileManager.CreateCellmFilePath(CreateModelFileName(x.Key), "Llamafile"));

            // Start server
            var baseAddress = new UriBuilder(
                _llamafileConfiguration.BaseAddress.Scheme,
                _llamafileConfiguration.BaseAddress.Host,
                _serverManager.FindPort(),
                _llamafileConfiguration.BaseAddress.AbsolutePath).Uri;

            var process = await StartProcess(exePath, modelPath, baseAddress);

            return new Llamafile(modelPath, baseAddress, process);
        }));
    }

    public async Task<LlamafileResponse> Handle(LlamafileRequest request, CancellationToken cancellationToken)
    {
        // Start server on first call
        var llamafile = await _llamafiles[request.Prompt.Options.ModelId ?? _llamafileConfiguration.DefaultModel];

        var openAiResponse = await _sender.Send(new OpenAiCompatibleRequest(request.Prompt, llamafile.BaseAddress), cancellationToken);

        return new LlamafileResponse(openAiResponse.Prompt);
    }

    private async Task<Process> StartProcess(string exePath, string modelPath, Uri baseAddress)
    {
        var processStartInfo = new ProcessStartInfo(exePath);

        processStartInfo.ArgumentList.Add("--server");
        processStartInfo.ArgumentList.Add("--nobrowser");
        processStartInfo.ArgumentList.Add("-m");
        processStartInfo.ArgumentList.Add(modelPath);
        processStartInfo.ArgumentList.Add("--host");
        processStartInfo.ArgumentList.Add(baseAddress.Host);
        processStartInfo.ArgumentList.Add("--port");
        processStartInfo.ArgumentList.Add(baseAddress.Port.ToString());

        if (_llamafileConfiguration.Gpu)
        {
            processStartInfo.Arguments += $"-ngl {_llamafileConfiguration.GpuLayers} ";
        }

        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.RedirectStandardOutput = true;

        var process = Process.Start(processStartInfo) ?? throw new CellmModelException("Failed to run Llamafile");

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogDebug(e.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var uriBuilder = new UriBuilder(baseAddress.Scheme, baseAddress.Host, baseAddress.Port, "/health");
        await _serverManager.WaitForServer(uriBuilder.Uri, process);

        // Kill Llamafile when Excel exits or dies
        _processManager.AssignProcessToExcel(process);

        return process;
    }

    private static string CreateModelFileName(string modelName)
    {
        return $"Llamafile-{modelName}";
    }
}

