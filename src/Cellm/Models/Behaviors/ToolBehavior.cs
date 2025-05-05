using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
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
    IOptionsMonitor<ProviderConfiguration> providerConfiguration,
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

        if (request.Prompt.Options.Tools is not null && request.Prompt.Options.Tools.Any())
        {
            logger.LogDebug("Tools: {tools}", request.Prompt.Options.Tools);
        }

        return await next();
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
                _mcpClientCache.TryGetValue(serverName, out var mcpClient);

                if (mcpClient is null)
                {
                    var clientTransport = new StdioClientTransport(serverConfiguration);
                    mcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
                }

                serverTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
                _mcpClientToolCache[serverName] = serverTools;
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
                _mcpClientCache.TryGetValue(serverName, out var mcpClient);

                if (mcpClient is null)
                {
                    var clientTransport = new SseClientTransport(serverConfiguration);
                    mcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
                }

                serverTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
                _mcpClientToolCache[serverName] = serverTools;
            }

            foreach (var serverTool in serverTools)
            {
                yield return serverTool;
            }
        }
    }
}
