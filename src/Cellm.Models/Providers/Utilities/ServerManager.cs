using System.Diagnostics;
using System.Net.NetworkInformation;
using Cellm.AddIn.Exceptions;

namespace Cellm.Models.Local.Utilities;

internal class ServerManager(HttpClient httpClient)
{
    public async Task WaitForServer(Uri endpoint, Process process, int timeOutInSeconds = 30)
    {
        var startTime = DateTime.UtcNow;

        // Wait max 30 seconds to load model
        while ((DateTime.UtcNow - startTime).TotalSeconds < timeOutInSeconds)
        {
            if (process.HasExited)
            {
                throw new CellmException($"Process exited early: {endpoint}");
            }

            try
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var response = await httpClient.GetAsync(endpoint, cancellationTokenSource.Token);

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
            await Task.Delay(100);
        }

        process.Kill();

        throw new CellmException("Failed to run Llamafile, timeout waiting for Llamafile server to start");
    }

    public int FindPort(ushort min = 49152, ushort max = 65535)
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
