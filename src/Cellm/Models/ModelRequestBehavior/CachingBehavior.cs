using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cellm.AddIn;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cellm.Models.ModelRequestBehavior;

internal class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;

    public CachingBehavior(IMemoryCache memoryCache, IOptions<CellmConfiguration> _cellmConfiguration)
    {
        _memoryCache = memoryCache;
        _memoryCacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromSeconds(_cellmConfiguration.Value.CacheTimeoutInSeconds)
        };
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var key = GetKey(request);

        if (_memoryCache.TryGetValue(key, out object? value) && value is TResponse response)
        {
            return response;
        }

        response = await next();

        _memoryCache.Set(key, response, _memoryCacheEntryOptions);

        return response;
    }

    private static string GetKey<T>(T key)
    {
        var json = JsonSerializer.Serialize(key);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        var hash = Convert.ToHexString(bytes);

        return hash;
    }
}
