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
    private static readonly List<string> _tag = [nameof(Account)];

    private readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(accountConfiguration.CurrentValue.CacheTimeoutInSeconds)
    };

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    internal async Task<bool> HasEntitlementAsync(Entitlement entitlement, CancellationToken cancellationToken)
    {
        if (!accountConfiguration.CurrentValue.IsEnabled)
        {
            return true;
        }

        var entitlements = await cache.GetOrCreateAsync(
            nameof(GetEntitlements) + accountConfiguration.CurrentValue.ApiKey,
            async innerCancellationToken => await GetEntitlements(innerCancellationToken),
            options: _cacheEntryOptions,
            _tag,
            cancellationToken: cancellationToken
        );

        return entitlements?.AsEnumerable().Contains(entitlement) ?? false;
    }

    internal bool HasEntitlement(Entitlement entitlement)
    {
        return Task.Run(async () => await HasEntitlementAsync(entitlement, CancellationToken.None)).GetAwaiter().GetResult();
    }

    internal async Task ThrowIfNotEntitledAsync(Entitlement entitlement, CancellationToken cancellationToken)
    {
        var hasEntitlement = await HasEntitlementAsync(entitlement, cancellationToken);

        if (!hasEntitlement)
        {
            throw new PermissionDeniedException(entitlement);
        }
    }

    internal void ThrowIfNotEntitled(Entitlement entitlement)
    {
        Task.Run(async () => await ThrowIfNotEntitledAsync(entitlement, CancellationToken.None)).GetAwaiter().GetResult();
    }

    internal async Task<string> GetTokenAsync(string username, string password, CancellationToken cancellationToken)
    {
        var baseAddress = accountConfiguration.CurrentValue.BaseAddress.ToString().TrimEnd('/');

        try
        {
            logger.LogInformation("Getting token for {username} ...", username);
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{baseAddress}/user/token"));
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

    private async Task<bool> CheckTokenAsync(string token, CancellationToken cancellationToken)
    {
        var baseAddress = accountConfiguration.CurrentValue.BaseAddress.ToString().TrimEnd('/');

        try
        {
            logger.LogInformation("Checking token for {email} ...", accountConfiguration.CurrentValue.Email);
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{baseAddress}/user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogInformation("Checking token for {email} ... {status}", accountConfiguration.CurrentValue.Email, response.StatusCode);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal async Task<bool> HasValidTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return await cache.GetOrCreateAsync(
            nameof(HasValidTokenAsync) + token,
            async innerCancellationToken => await CheckTokenAsync(token, innerCancellationToken),
            options: _cacheEntryOptions,
            _tag,
            cancellationToken: CancellationToken.None
        );
    }

    internal bool HasValidToken(string token)
    {
        return Task.Run(() => HasValidTokenAsync(token)).GetAwaiter().GetResult();
    }

    internal async void ClearAsync(CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(_tag, cancellationToken);
    }

    internal void Clear()
    {
        Task.Run(() => ClearAsync(CancellationToken.None)).GetAwaiter().GetResult();
    }

    private async Task<Entitlements> GetEntitlements(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountConfiguration.CurrentValue.ApiKey))
        {
            // API key is definitely not valid, immediately return default entitlements
            return new Entitlements();
        }

        var baseAddress = accountConfiguration.CurrentValue.BaseAddress.ToString().TrimEnd('/');

        try
        {
            logger.LogInformation("Getting entitlements for {email} ...", accountConfiguration.CurrentValue.Email);
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{baseAddress}/user/permissions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accountConfiguration.CurrentValue.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            logger.LogInformation("Getting entitlements for {email} ... {status}", accountConfiguration.CurrentValue.Email, response.StatusCode);

            // If anything goes wrong for ANY reason, we want to return default entitlements
            // so anonymous users can use the application as intended. We help the user identify
            // that credentials are invalid elsewhere
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Deserializing entitlements for {email} ...", accountConfiguration.CurrentValue.Email);
            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var entitlements = await JsonSerializer.DeserializeAsync<Entitlements>(contentStream, _jsonSerializerOptions, cancellationToken);

            if (entitlements is null)
            {
                throw new NullReferenceException(nameof(entitlements));
            }

            logger.LogInformation("Deserializing entitlements for {email} ... Done: {@entitlements}",
                accountConfiguration.CurrentValue.Email, entitlements.AsEnumerable());

            return entitlements;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("Getting entitlements for {email} ... Failed: {message}", accountConfiguration.CurrentValue.Email, ex.Message);
            return new Entitlements();
        }
        catch (JsonException ex)
        {
            logger.LogError("Deserializing entitlements for {email} ... Failed: {message}", accountConfiguration.CurrentValue.Email, ex.Message);
            return new Entitlements();
        }
        catch (Exception ex)
        {
            logger.LogError("An unexpected error occurred while getting user entitlements: {message}", ex.Message);
            return new Entitlements();
        }
    }

    private static string GetBasicAuthBase64(string username, string password)
    {
        var credentials = $"{username}:{password}";
        var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
        var credentialsAsBase64 = Convert.ToBase64String(credentialsBytes);

        return credentialsAsBase64;
    }
}
