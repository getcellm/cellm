using System.Diagnostics;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Cellm.Models.OpenAi;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Llamafile;

internal class LlamafileClient : IClient
{
    private readonly AsyncLazy<string> _llamafilePath;
    private readonly AsyncLazy<string> _llamafileModelPath;
    private readonly AsyncLazy<Process> _llamafileProcess;

    private readonly CellmConfiguration _cellmConfiguration;
    private readonly LlamafileConfiguration _llamafileConfiguration;
    private readonly OpenAiConfiguration _openAiConfiguration;

    private readonly IClient _openAiClient;
    private readonly HttpClient _httpClient;
    private readonly LLamafileProcessManager _llamafileProcessManager;

    public LlamafileClient(IOptions<CellmConfiguration> cellmConfiguration,
        IOptions<LlamafileConfiguration> llamafileConfiguration,
        IOptions<OpenAiConfiguration> openAiConfiguration,
        IClientFactory clientFactory,
        HttpClient httpClient,
        LLamafileProcessManager llamafileProcessManager)
    {
        _cellmConfiguration = cellmConfiguration.Value;
        _llamafileConfiguration = llamafileConfiguration.Value;
        _openAiConfiguration = openAiConfiguration.Value;
        _openAiClient = clientFactory.GetClient("openai");
        _httpClient = httpClient;
        _llamafileProcessManager = llamafileProcessManager;

        _llamafilePath = new AsyncLazy<string>(async () =>
         {
             return await DownloadFile(_llamafileConfiguration.LlamafileUrl, $"Llamafile.exe", httpClient);
         });

        _llamafileModelPath = new AsyncLazy<string>(async () =>
        {
            return await DownloadFile(_llamafileConfiguration.Models[_llamafileConfiguration.DefaultModel], $"Llamafile-model-weights-{_llamafileConfiguration.DefaultModel}", httpClient);
        });

        _llamafileProcess = new AsyncLazy<Process>(async () =>
        {
            return await StartProcess();
        });
    }

    public async Task<Prompt> Send(Prompt prompt, string? provider, string? model)
    {
        await _llamafilePath;
        await _llamafileModelPath;
        await _llamafileProcess;
        return await _openAiClient.Send(prompt, provider, model);
    }

    private async Task<Process> StartProcess()
    {
        var processStartInfo = new ProcessStartInfo(await _llamafilePath);
        processStartInfo.Arguments += $"-m {await _llamafileModelPath} ";
        processStartInfo.Arguments += $"--port {_llamafileConfiguration.Port} ";

        if (!_cellmConfiguration.Debug)
        {
            processStartInfo.Arguments += "--disable-browser ";
        }

        if (_llamafileConfiguration.Gpu)
        {
            processStartInfo.Arguments += $"-ngl {_llamafileConfiguration.GpuLayers} ";
        }

        var process = Process.Start(processStartInfo) ?? throw new CellmException("Failed to start Llamafile server");

        try
        {
            Thread.Sleep(5000);
            // await WaitForLlamafile(process);
            _llamafileProcessManager.AssignProcessToCellm(process);
            return process;
        }
        catch
        {
            process.Kill();
            throw;
        }
    }

    private static async Task<string> DownloadFile(Uri uri, string filename, HttpClient httpClient)
    {
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Cellm), filename);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new CellmException("Failed to create Llamafile path"));

        if (File.Exists(filePath))
        {
            return filePath;
        }

        var filePathPart = filePath + ".part";

        if (File.Exists(filePathPart))
        {
            File.Delete(filePathPart);
        }

        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using (var fileStream = File.Create(filePathPart))
        using (var httpStream = await response.Content.ReadAsStreamAsync())
        {

            await httpStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }

        File.Move(filePathPart, filePath);

        return filePath;
    }

    private async Task WaitForLlamafile(Process process)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var startTime = DateTime.UtcNow;

        // Max 30 seconds timeout
        while ((DateTime.UtcNow - startTime).TotalSeconds < 30)
        {
            if (process.HasExited)
            {
                throw new CellmException($"Failed to run Llamafile. Exit code: {process.ExitCode}");
            }

            try
            {
                var response = await _httpClient.GetAsync(new Uri(_openAiConfiguration.BaseAddress, "health"), cancellationTokenSource.Token);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Server is ready
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            // Wait before next attempt
            await Task.Delay(500);
        }

        process.Kill();
        throw new CellmException("Timeout waiting for Llamafile server to be ready");
    }
}

