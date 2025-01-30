using System.Text.Json;
using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class CacheBehavior<TRequest, TResponse>(HybridCache cache, IOptionsMonitor<ProviderConfiguration> providerConfiguration) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(providerConfiguration.CurrentValue.CacheTimeoutInSeconds)
    };

    private static List<string> Tags = [nameof(IModelResponse)];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (providerConfiguration.CurrentValue.EnableCache)
        {
            return await cache.GetOrCreateAsync(
                JsonSerializer.Serialize(request.Prompt),
                async cancel => await next(),
                options: _cacheEntryOptions,
                Tags,
                cancellationToken: cancellationToken
            );
        }

        return await next();
    }
}
