using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cellm.AddIn;
using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class CacheBehavior<TRequest, TResponse>(
    HybridCache cache,
    IOptionsMonitor<CellmAddInConfiguration> providerConfiguration,
    ILogger<CacheBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IGetPrompt
    where TResponse : IGetPrompt
{
    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(providerConfiguration.CurrentValue.CacheTimeoutInSeconds)
    };

    private static readonly List<string> Tags = [nameof(ProviderResponse)];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!providerConfiguration.CurrentValue.EnableCache)
        {
            logger.LogDebug("Prompt caching disabled");
            return await next().ConfigureAwait(false);
        }

        logger.LogDebug("Prompt caching enabled");

        var promptAsJsonString = JsonSerializer.Serialize(request.Prompt);

        // ChatOptions.Tools are explicitly [JsonIgnore]'d so we manually add
        // them to the key to ensure cache miss if user adds/removes tools
        var toolsAsJsonString = JsonSerializer.Serialize(request.Prompt.Options.Tools);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(promptAsJsonString + toolsAsJsonString));
        var key = Convert.ToBase64String(hash);

        return await cache.GetOrCreateAsync(
            key,
            async innerCancellationToken => await next().ConfigureAwait(false),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken
        ).ConfigureAwait(false);
    }
}
