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
using System.Text;
using System.Text.Json;
using Cellm.Users.Exceptions;
using Cellm.Users.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            nameof(GetEntitlements) + GetBasicAuthCredentials(),
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

    internal async Task RequireEntitlementAsync(Entitlement entitlement)
    {
        var hasEntitlement = await HasEntitlementAsync(entitlement);

        if (!hasEntitlement)
        {
            throw new PermissionDeniedException(entitlement);
        }
    }

    internal void ThrowIfNotEntitled(Entitlement entitlement)
    {
        Task.Run(async () => await RequireEntitlementAsync(entitlement)).GetAwaiter().GetResult();
    }

    public bool HasBasicAuthCredentials()
    {
        var username = accountConfiguration.CurrentValue.Username;
        var password = accountConfiguration.CurrentValue.Password;

        return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
    }

    public string GetBasicAuthCredentials(string? username = null, string? password = null)
    {
        username ??= accountConfiguration.CurrentValue.Username;
        password ??= accountConfiguration.CurrentValue.Password;

        var credentials = $"{username}:{password}";
        var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
        var credentialsAsBase64 = Convert.ToBase64String(credentialsBytes);

        return credentialsAsBase64;
    }

    internal async Task<bool> HasValidCredentialsAsync(string username, string password)
    {
        return await cache.GetOrCreateAsync(
            nameof(HasValidCredentialsAsync) + GetBasicAuthCredentials(username, password),
            async innerCancellationToken => await CheckCredentialsAsync(username, password, innerCancellationToken),
            options: _cacheEntryOptions,
            Tags,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<bool> CheckCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var uri = accountConfiguration.CurrentValue.BaseAddress;

        if (!uri.AbsoluteUri.EndsWith('/'))
        {
            uri = new Uri(uri.AbsoluteUri + "/");
        }

        try
        {
            logger.LogInformation("Checking credentials for {username} ...", username);
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri, "user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetBasicAuthCredentials(username, password));
            var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogInformation("Checking credentials for {username} ... {status}", username, response.StatusCode);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<ActiveEntitlements> GetEntitlements(CancellationToken cancellationToken)
    {
        var uri = accountConfiguration.CurrentValue.BaseAddress;

        if (!uri.AbsoluteUri.EndsWith("/"))
        {
            uri = new Uri(uri.AbsoluteUri + "/");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(uri, "user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetBasicAuthCredentials());
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
