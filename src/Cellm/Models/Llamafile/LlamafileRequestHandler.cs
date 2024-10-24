﻿using System.Diagnostics;
using System.Net.NetworkInformation;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.OpenAi;
using MediatR;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Llamafile;

internal class LlamafileRequestHandler : IProviderRequestHandler<LlamafileRequest, LlamafileResponse>
{
    private record Llamafile(string ModelPath, Uri BaseAddress, Process Process);

    private readonly AsyncLazy<string> _llamafileExePath;
    private readonly Dictionary<string, AsyncLazy<Llamafile>> _llamafiles;
    private readonly LLamafileProcessManager _llamafileProcessManager;

    private readonly CellmConfiguration _cellmConfiguration;
    private readonly LlamafileConfiguration _llamafileConfiguration;
    private readonly OpenAiConfiguration _openAiConfiguration;

    private readonly ISender _sender;
    private readonly HttpClient _httpClient;

    public LlamafileRequestHandler(IOptions<CellmConfiguration> cellmConfiguration,
        IOptions<LlamafileConfiguration> llamafileConfiguration,
        IOptions<OpenAiConfiguration> openAiConfiguration,
        ISender sender,
        HttpClient httpClient,
        LLamafileProcessManager llamafileProcessManager)
    {
        _cellmConfiguration = cellmConfiguration.Value;
        _llamafileConfiguration = llamafileConfiguration.Value;
        _openAiConfiguration = openAiConfiguration.Value;
        _sender = sender;
        _httpClient = httpClient;
        _llamafileProcessManager = llamafileProcessManager;

        _llamafileExePath = new AsyncLazy<string>(async () =>
        {
            return await DownloadFile(_llamafileConfiguration.LlamafileUrl, $"{nameof(Llamafile)}.exe");
        });

        _llamafiles = _llamafileConfiguration.Models.ToDictionary(x => x.Key, x => new AsyncLazy<Llamafile>(async () =>
        {
            // Download model
            var modelPath = await DownloadFile(x.Value, CreateFilePath(CreateModelFileName(x.Key)));

            // Run Llamafile
            var baseAddress = CreateBaseAddress();
            var process = await StartProcess(modelPath, baseAddress);

            return new Llamafile(modelPath, baseAddress, process);
        }));
    }

    public async Task<LlamafileResponse> Handle(LlamafileRequest request, CancellationToken cancellationToken)
    {
        // Download model and start Llamafile on first call
        var llamafile = await _llamafiles[request.Prompt.Model ?? _llamafileConfiguration.DefaultModel];

        var openAiResponse = await _sender.Send(new OpenAiRequest(request.Prompt, nameof(Llamafile), llamafile.BaseAddress), cancellationToken);

        return new LlamafileResponse(openAiResponse.Prompt);
    }

    private async Task<Process> StartProcess(string modelPath, Uri baseAddress)
    {
        var processStartInfo = new ProcessStartInfo(await _llamafileExePath);

        processStartInfo.Arguments += $"--server ";
        processStartInfo.Arguments += "--nobrowser ";
        processStartInfo.Arguments += $"-m {modelPath} ";
        processStartInfo.Arguments += $"--host {baseAddress.Host} ";
        processStartInfo.Arguments += $"--port {baseAddress.Port} ";

        if (_llamafileConfiguration.Gpu)
        {
            processStartInfo.Arguments += $"-ngl {_llamafileConfiguration.GpuLayers} ";
        }

        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardError = _cellmConfiguration.Debug;
        processStartInfo.RedirectStandardOutput = _cellmConfiguration.Debug;

        var process = Process.Start(processStartInfo) ?? throw new CellmException("Failed to run Llamafile");

        if (_cellmConfiguration.Debug)
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.WriteLine(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        await WaitForLlamafile(baseAddress, process);

        // Kill the process when Excel exits or dies
        _llamafileProcessManager.AssignProcessToExcel(process);

        return process;
    }

    private async Task<string> DownloadFile(Uri uri, string filePath)
    {
        if (File.Exists(filePath))
        {
            return filePath;
        }

        var filePathPart = $"{filePath}.part";

        if (File.Exists(filePathPart))
        {
            File.Delete(filePathPart);
        }

        var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using (var fileStream = File.Create(filePathPart))
        using (var httpStream = await response.Content.ReadAsStreamAsync())
        {

            await httpStream.CopyToAsync(fileStream);
        }

        File.Move(filePathPart, filePath);

        return filePath;
    }

    private async Task WaitForLlamafile(Uri baseAddress, Process process)
    {
        var startTime = DateTime.UtcNow;

        // Wait max 30 seconds to load model
        while ((DateTime.UtcNow - startTime).TotalSeconds < 30)
        {
            if (process.HasExited)
            {
                throw new CellmException($"Failed to run Llamafile, process exited. Exit code: {process.ExitCode}");
            }

            try
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var response = await _httpClient.GetAsync(new Uri(baseAddress, "health"), cancellationTokenSource.Token);
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

        throw new CellmException("Failed to run Llamafile, timeout waiting for Llamafile server to start");
    }

    string CreateFilePath(string fileName)
    {
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Cellm), fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new CellmException("Failed to create Llamafile folder"));
        return filePath;
    }

    private static string CreateModelFileName(string modelName)
    {
        return $"Llamafile-model-{modelName}";
    }

    private Uri CreateBaseAddress()
    {
        var uriBuilder = new UriBuilder(_llamafileConfiguration.BaseAddress)
        {
            Port = GetFirstUnusedPort()
        };

        return uriBuilder.Uri;
    }

    private static int GetFirstUnusedPort(ushort min = 49152, ushort max = 65535)
    {
        if (max < min)
        {
            throw new ArgumentException("Max port must be larger than min port.");
        }

        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();

        var activePorts = ipProperties.GetActiveTcpConnections()
            .Where(connection => connection.State != TcpState.Closed)
            .Select(connection => connection.LocalEndPoint)
            .Concat(ipProperties.GetActiveTcpListeners())
            .Concat(ipProperties.GetActiveUdpListeners())
            .Select(endpoint => endpoint.Port)
            .ToArray();

        var firstInactivePort = Enumerable.Range(min, max)
            .Where(port => !activePorts.Contains(port))
            .FirstOrDefault();

        if (firstInactivePort == default)
        {
            throw new CellmException($"All local TCP ports between {min} and {max} are currently in use.");
        }

        return firstInactivePort;
    }
}

