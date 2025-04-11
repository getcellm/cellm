using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class CacheBehavior<TRequest, TResponse>(
    HybridCache cache,
    IOptionsMonitor<ProviderConfiguration> providerConfiguration,
    ILogger<CacheBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(providerConfiguration.CurrentValue.CacheTimeoutInSeconds)
    };

    private static readonly List<string> Tags = [nameof(IModelResponse)];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!providerConfiguration.CurrentValue.EnableCache)
        {
            logger.LogDebug("Prompt caching disabled");
            return await next();
        }

        logger.LogDebug("Prompt caching enabled");

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.Prompt)));
        var key = Convert.ToBase64String(hashBytes);

        return await cache.GetOrCreateAsync(
            key,
            async innerCancellationToken => await next(),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken
        );
    }
}
