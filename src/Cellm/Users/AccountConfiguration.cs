namespace Cellm.Users;

internal class AccountConfiguration
{
    public Uri BaseAddress { get; init; } = default!;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int CacheTimeoutInSeconds { get; init; }

    public bool IsEnabled { get; init; } = false;
}
