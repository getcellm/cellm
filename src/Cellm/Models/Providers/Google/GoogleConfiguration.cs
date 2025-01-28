namespace Cellm.Models.Providers.Google;

internal class GoogleConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}