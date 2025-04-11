using System.Runtime.CompilerServices;
using Cellm.Models.Providers;
using Cellm.Tools.ModelContextProtocol;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.Models.Tools;

internal class ToolBehavior<TRequest, TResponse>(
    IOptionsMonitor<ProviderConfiguration> providerConfiguration,
    IOptionsMonitor<ModelContextProtocolConfiguration> modelContextProtocolConfiguration,
    IEnumerable<AIFunction> functions,
    ILogger<ToolBehavior<TRequest, TResponse>> logger,
    ILoggerFactory loggerFactory)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IModelRequest<TResponse>
{
    // TODO: Use HybridCache (await fix that McpLcientTool can't be serialized/deserialized)
    private Dictionary<string, IList<McpClientTool>> _poorMansCache = [];

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

        if (providerConfiguration.CurrentValue.EnableModelContextProtocolServers.Any(t => t.Value))
        {
            logger.LogDebug("MCP tools enabled");

            request.Prompt.Options.Tools ??= [];

            await foreach (var tool in GetModelContextProtocolTools(cancellationToken))
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
        return functions
                .Where(f => providerConfiguration.CurrentValue.EnableTools[f.Name])
                .ToList<AITool>();
    }

    // TODO: Query servers in parallel

    private async IAsyncEnumerable<AITool> GetModelContextProtocolTools([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var server in modelContextProtocolConfiguration.CurrentValue.Servers)
        {
            if (!providerConfiguration.CurrentValue.EnableModelContextProtocolServers.TryGetValue(server.Name, out var isEnabled) || !isEnabled)
            {
                continue;
            }

            _poorMansCache.TryGetValue(server.Name, out var tools);

            if (tools is null)
            {
                var client = await McpClientFactory.CreateAsync(server, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
                tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                _poorMansCache[server.Name] = tools;
            }

            foreach (var tool in tools)
            {
                yield return tool;
            }
        }
    }
}
