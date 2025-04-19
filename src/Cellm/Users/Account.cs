// Circumventing limitations anywhere in the codebase is a direct violation of the
// Fair Core License, Version 1.0 until a commit transitions to the Apache 2.0 Future License.
// Once a commit transitions to the Apache 2.0 Future License, you can checkout out
// that commit and use the code under the Apache 2.0 License. A commit transitions to
// the Apache 2.0 Future License on the second anniversary of the date the git commit
// became available. 
//
// The relevant section of the Fair Core License, Version 1.0 is:
//
// > ### Limitations
// > You must not move, change, disable, or circumvent the license key functionality
// > in the Software; or modify any portion of the Software protected by the license
// > key to:
//
// > 1. enable access to the protected functionality without a valid license key; or
//
// > 2. remove the protected functionality.
//
// You can checkout the latest commit licensed under the Apache 2.0 License like this:
// 
// $ git checkout $(git rev-list -n 1 --before="2 years ago" HEAD)
//
// For more details, go to https://github.com/getcellm/cellm/blob/main/LICENSE.

using System.Text;
using System.Text.Json;
using Cellm.User.Exceptions;
using Cellm.User.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.User;

internal class Account(
    IOptionsMonitor<AccountConfiguration> accountConfiguration,
    HybridCache cache,
    [FromKeyedServices("ResilientHttpClient")] HttpClient httpClient,
    ILogger<Account> logger)
{
    private static readonly List<string> Tags = [nameof(Account)];

    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(accountConfiguration.CurrentValue.CacheTimeoutInSeconds)
    };

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    internal async Task<bool> HasEntitlementAsync(Entitlement entitlement)
    {
        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return true;
        }

        var entitlements = await cache.GetOrCreateAsync(
            nameof(GetEntitlements) + GetCredentials(),
            async innerCancellationToken => await GetEntitlements(innerCancellationToken),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken: CancellationToken.None
        );

        return entitlements?.Entitlements.Contains(entitlement) ?? false;
    }

    internal async Task RequireEntitlementAsync(Entitlement entitlement)
    {
        var hasEntitlement = await HasEntitlementAsync(entitlement);

        if (!hasEntitlement)
        {
            throw new PermissionDeniedException(entitlement);
        }
    }

    internal void RequireEntitlement(Entitlement entitlement)
    {
        Task.Run(async () => await RequireEntitlementAsync(entitlement)).GetAwaiter().GetResult();
    }

    public string GetCredentials()
    {
        var credentials = $"{accountConfiguration.CurrentValue.Username}:{accountConfiguration.CurrentValue.Password}";
        var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
        var credentialsAsBase64 = Convert.ToBase64String(credentialsBytes);
        return credentialsAsBase64;
    }

    private async Task<ActiveEntitlements> GetEntitlements(CancellationToken cancellationToken)
    {

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(accountConfiguration.CurrentValue.BaseAddress, "user/permissions"));
        request.Headers.Add("Authorization", $"Basic {GetCredentials()}");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);

            // If anything goes wrong for ANY reason, we want to return default entitlements
            // so anonymous users can use the application as intended. We help the user identify
            // invalid credentials elsewhere
            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var activeEntitlements = await JsonSerializer.DeserializeAsync<ActiveEntitlements>(contentStream, _jsonSerializerOptions, cancellationToken);

            if (activeEntitlements is null)
            {
                throw new NullReferenceException(nameof(activeEntitlements));
            }

            return activeEntitlements;
        }
        catch (JsonException ex)
        {
            logger.LogError("Failed to deserialize user entitlements: {message}", ex.Message);
            return new ActiveEntitlements();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Failed to fetch entitlements: {message}", ex.Message);
            return new ActiveEntitlements();
        }
        catch (Exception ex)
        {
            logger.LogError("An unexpected error occurred while getting user entitlements: {message}", ex.Message);
            return new ActiveEntitlements();
        }
    }
}
