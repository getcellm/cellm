using System.Text.Json;
using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class CacheBehavior<TRequest, TResponse>(HybridCache cache, IOptions<ProviderConfiguration> providerConfiguration) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(providerConfiguration.Value.CacheTimeoutInSeconds)
    };

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (providerConfiguration.Value.EnableCache)
        {
            return await cache.GetOrCreateAsync(
                JsonSerializer.Serialize(request.Prompt),
                async cancel => await next(),
                options: _cacheEntryOptions,
                cancellationToken: cancellationToken
            );
        }

        return await next();
    }
}
