﻿using Cellm.Models.Providers.Behaviors;
using MediatR;

namespace Cellm.Models.Behaviors;

internal class ProviderBehavior<TRequest, TResponse>(IEnumerable<IProviderBehavior> providerBehaviors) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IGetPrompt, IGetProvider
    where TResponse : IGetPrompt, IGetProvider
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var enabledProviderBehaviors = providerBehaviors
            .Where(a => a.IsEnabled(request.Provider))
            .OrderBy(a => a.Order);

        foreach (var providerBehavior in enabledProviderBehaviors)
        {
            providerBehavior.Before(request.Prompt);
        }

        var response = await next().ConfigureAwait(false);

        foreach (var providerBehavior in enabledProviderBehaviors)
        {
            providerBehavior.After(response.Prompt);
        }

        return response;
    }
}
