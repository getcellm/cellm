using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cellm.AddIn;
using Cellm.Models.Prompts;
using Cellm.Tools.ModelContextProtocol;
using Cellm.Users;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace Cellm.Models.Behaviors;

internal class ToolBehavior<TRequest, TResponse>(
    Account account,
    IOptionsMonitor<CellmAddInConfiguration> providerConfiguration,
    IOptionsMonitor<ModelContextProtocolConfiguration> modelContextProtocolConfiguration,
    IEnumerable<AIFunction> functions,
    ILogger<ToolBehavior<TRequest, TResponse>> logger,
    ILoggerFactory loggerFactory)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IPrompt
{
    // TODO: Use HybridCache
    private readonly ConcurrentDictionary<string, IMcpClient> _mcpClientCache = [];
    private readonly ConcurrentDictionary<string, IList<McpClientTool>> _mcpClientToolCache = [];
    private readonly SemaphoreSlim _asyncLock = new(1, 1);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (providerConfiguration.CurrentValue.EnableTools.Any(t => t.Value))
        {
            logger.LogDebug("Native tools enabled");

            request.Prompt.Options.Tools = GetNativeTools();
        }
        else
        {
            logger.LogDebug("Native tools disabled");
        }

        var enableModelContextProtocol = await account.HasEntitlementAsync(Entitlement.EnableModelContextProtocol);

        if (providerConfiguration.CurrentValue.EnableModelContextProtocolServers.Any(t => t.Value) && enableModelContextProtocol)
        {
            logger.LogDebug("MCP tools enabled");

            request.Prompt.Options.Tools ??= [];

            await foreach (var tool in GetModelContextProtocolToolsAsync(cancellationToken))
            {
                request.Prompt.Options.Tools.Add(tool);
            }
        }
        else
        {
            logger.LogDebug("MCP tools disabled");
        }

        if (request.Prompt.Options.Tools?.Any() ?? false)
        {
            logger.LogDebug("Tools: {tools}", request.Prompt.Options.Tools);
        }

        return await next().ConfigureAwait(false);
    }

    private List<AITool> GetNativeTools()
    {
        return [.. functions.Where(f => providerConfiguration.CurrentValue.EnableTools[f.Name])];
    }

    // TODO: Query servers in parallel
    private async IAsyncEnumerable<AITool> GetModelContextProtocolToolsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var serverConfiguration in modelContextProtocolConfiguration.CurrentValue.StdioServers)
        {
            var serverName = serverConfiguration.Name ?? throw new NullReferenceException(nameof(serverConfiguration.Name));

            if (!providerConfiguration.CurrentValue.EnableModelContextProtocolServers.TryGetValue(serverName, out var isEnabled) || !isEnabled)
            {
                continue;
            }

            _mcpClientToolCache.TryGetValue(serverName, out var serverTools);

            if (serverTools is null)
            {
                await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    _mcpClientCache.TryGetValue(serverName, out var StdioMcpClient);

                    if (StdioMcpClient is null)
                    {
                        var clientTransport = new StdioClientTransport(serverConfiguration);
                        StdioMcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken).ConfigureAwait(false);
                        _mcpClientCache[serverName] = StdioMcpClient;
                    }

                    serverTools = await StdioMcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    _mcpClientToolCache[serverName] = serverTools;
                }
                finally
                {
                    _asyncLock.Release();
                }
            }

            foreach (var serverTool in serverTools)
            {
                yield return serverTool;
            }
        }

        foreach (var serverConfiguration in modelContextProtocolConfiguration.CurrentValue.SseServers)
        {
            var serverName = serverConfiguration.Name ?? throw new NullReferenceException(nameof(serverConfiguration.Name));

            if (!providerConfiguration.CurrentValue.EnableModelContextProtocolServers.TryGetValue(serverName, out var isEnabled) || !isEnabled)
            {
                continue;
            }

            _mcpClientToolCache.TryGetValue(serverName, out var serverTools);

            if (serverTools is null)
            {
                await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    _mcpClientCache.TryGetValue(serverName, out var SseMcpClient);

                    if (SseMcpClient is null)
                    {
                        var clientTransport = new SseClientTransport(serverConfiguration);
                        SseMcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken).ConfigureAwait(false);
                        _mcpClientCache[serverName] = SseMcpClient;
                    }

                    serverTools = await SseMcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    _mcpClientToolCache[serverName] = serverTools;
                }
                finally
                {
                    _asyncLock.Release();
                }
            }

            foreach (var serverTool in serverTools)
            {
                yield return serverTool;
            }
        }
    }
}
