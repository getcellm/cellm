namespace Cellm.Users;

internal class AccountConfiguration
{
    public Uri BaseAddress { get; init; } = new Uri("https://getcellm.com/v1/");

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public int CacheTimeoutInSeconds { get; init; }

    public bool IsEnabled => false;
}
