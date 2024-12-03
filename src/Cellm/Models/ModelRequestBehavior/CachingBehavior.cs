using System.Text.Json;
using Cellm.Services.Configuration;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Cellm.Models.ModelRequestBehavior;

internal class CachingBehavior<TRequest, TResponse>(HybridCache cache, IOptions<CellmConfiguration> cellmConfiguration) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(cellmConfiguration.Value.CacheTimeoutInSeconds)
    };

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (cellmConfiguration.Value.EnableCache)
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
