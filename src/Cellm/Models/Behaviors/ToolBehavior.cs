using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Tools;

internal class ToolBehavior<TRequest, TResponse>(IOptionsMonitor<ProviderConfiguration> providerConfiguration, IEnumerable<AIFunction> functions)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IModelRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (providerConfiguration.CurrentValue.EnableTools.Any(t => t.Value))
        {
            request.Prompt.Options.Tools = functions.Where(f => providerConfiguration.CurrentValue.EnableTools[f.Metadata.Name]).ToList<AITool>();
        }

        return await next();
    }
}
