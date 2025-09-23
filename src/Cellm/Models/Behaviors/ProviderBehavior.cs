using Cellm.Models.Providers.Behaviors;
using MediatR;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class ProviderBehavior<TRequest, TResponse>(IEnumerable<IProviderBehavior> providerBehaviors) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IGetPrompt, IGetProvider
    where TResponse : IGetPrompt
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var enabledProviderBehaviors = providerBehaviors
            .Where(providerBehavior => providerBehavior.IsEnabled(request.Provider))
            .OrderBy(providerBehavior => providerBehavior.Order);

        foreach (var providerBehavior in enabledProviderBehaviors)
        {
            providerBehavior.Before(request.Provider, request.Prompt);
        }

        var response = await next().ConfigureAwait(false);

        foreach (var providerBehavior in enabledProviderBehaviors)
        {
            providerBehavior.After(request.Provider, response.Prompt);
        }

        return response;
    }
}
