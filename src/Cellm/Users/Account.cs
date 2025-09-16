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

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Providers.Cellm;
using Cellm.Users.Exceptions;
using Cellm.Users.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig.Tokens;

namespace Cellm.Users;

internal class Account(
    IOptionsMonitor<AccountConfiguration> accountConfiguration,
    HybridCache cache,
    HttpClient httpClient,
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
            nameof(GetEntitlements) + accountConfiguration.CurrentValue.ApiKey,
            async innerCancellationToken => await GetEntitlements(innerCancellationToken),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken: CancellationToken.None
        );

        return entitlements?.Entitlements.Contains(entitlement) ?? false;
    }

    internal bool HasEntitlement(Entitlement entitlement)
    {
        return Task.Run(async () => await HasEntitlementAsync(entitlement)).GetAwaiter().GetResult();
    }

    internal async Task ThrowIfNotEntitledAsync(Entitlement entitlement)
    {
        var hasEntitlement = await HasEntitlementAsync(entitlement);

        if (!hasEntitlement)
        {
            throw new PermissionDeniedException(entitlement);
        }
    }

    internal void ThrowIfNotEntitled(Entitlement entitlement)
    {
        Task.Run(async () => await ThrowIfNotEntitledAsync(entitlement)).GetAwaiter().GetResult();
    }

    public string GetBasicAuthBase64(string username, string password)
    {
        var credentials = $"{username}:{password}";
        var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
        var credentialsAsBase64 = Convert.ToBase64String(credentialsBytes);

        return credentialsAsBase64;
    }

    internal async Task<string> GetTokenAsync(string username, string password, CancellationToken cancellationToken)
    {
        var uri = accountConfiguration.CurrentValue.BaseAddress;

        if (!uri.AbsoluteUri.EndsWith('/'))
        {
            uri = new Uri(uri.AbsoluteUri + "/");
        }

        try
        {
            logger.LogInformation("Getting token for {username} ...", username);
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(uri, "user/token"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetBasicAuthBase64(username, password));
            var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogInformation("Getting token for {username} ... {status}", username, response.StatusCode);
            response.EnsureSuccessStatusCode();

            var tokenPayload = await response.Content.ReadFromJsonAsync<TokenPayload>(_jsonSerializerOptions, cancellationToken) 
                ?? throw new NullReferenceException(nameof(TokenPayload));

            return tokenPayload.Token;
        }
        catch (Exception ex)
        {
            throw new CellmException("Could not get token", ex);
        }
    }

    internal async Task<bool> HasValidTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return await cache.GetOrCreateAsync(
            token,
            async innerCancellationToken => await CheckTokenAsync(token, innerCancellationToken),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<bool> CheckTokenAsync(string token, CancellationToken cancellationToken)
    {
        var uri = accountConfiguration.CurrentValue.BaseAddress;

        if (!uri.AbsoluteUri.EndsWith('/'))
        {
            uri = new Uri(uri.AbsoluteUri + "/");
        }

        try
        {
            logger.LogInformation("Checking bearer token ...");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri, "user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogInformation("Checking bearer token ... {status}", response.StatusCode);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<ActiveEntitlements> GetEntitlements(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountConfiguration.CurrentValue.ApiKey))
        {
            // API key is definitely not valid, return default entitlements
            return new ActiveEntitlements();
        }

        var uri = accountConfiguration.CurrentValue.BaseAddress;

        if (!uri.AbsoluteUri.EndsWith("/"))
        {
            uri = new Uri(uri.AbsoluteUri + "/");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri, "user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accountConfiguration.CurrentValue.ApiKey);
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
