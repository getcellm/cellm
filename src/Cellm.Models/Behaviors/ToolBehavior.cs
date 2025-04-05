using System.Runtime.CompilerServices;
using Cellm.Models.Providers;
using Cellm.Tools.ModelContextProtocol;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.Models.Tools;

internal class ToolBehavior<TRequest, TResponse>(
    IOptionsMonitor<ProviderConfiguration> providerConfiguration,
    IOptionsMonitor<ModelContextProtocolConfiguration> modelContextProtocolConfiguration,
    IEnumerable<AIFunction> functions)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IModelRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (providerConfiguration.CurrentValue.EnableTools.Any(t => t.Value))
        {
            request.Prompt.Options.Tools = GetNativeTools();
        }

        if (providerConfiguration.CurrentValue.EnableModelContextProtocolServers.Any(t => t.Value))
        {
            request.Prompt.Options.Tools ??= [];

            await foreach (var tool in GetModelContextProtocolTools(cancellationToken))
            {
                request.Prompt.Options.Tools.Add(tool);
            }
        }

        return await next();
    }

    private List<AITool> GetNativeTools()
    {
        return functions
                .Where(f => providerConfiguration.CurrentValue.EnableTools[f.Name])
                .ToList<AITool>();
    }

    // TODO:
    //  - Cache capabilities on a per-server basis.
    //  - Query servers in parallel.
    //
    //  Note: We cannot get list of tools only on startup because user can add/delete/enable/disable servers. But
    //        with this solution we query servers for capabilities on every model call which is hardly ideal.
    //        We need to cache on a per-server basis so servers added at runtime will be queried
    private async IAsyncEnumerable<AITool> GetModelContextProtocolTools([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var server in modelContextProtocolConfiguration.CurrentValue.Servers)
        {
            if (!providerConfiguration.CurrentValue.EnableModelContextProtocolServers.TryGetValue(server.Name, out var isEnabled) || !isEnabled)
            {
                continue;
            }

            var client = await McpClientFactory.CreateAsync(server, cancellationToken: cancellationToken);

            if (client is null)
            {
                continue;
            }

            foreach (var tool in await client.ListToolsAsync(cancellationToken: cancellationToken))
            {
                yield return tool;
            }
        }
    }
}
