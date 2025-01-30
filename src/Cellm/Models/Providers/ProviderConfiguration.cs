namespace Cellm.Models.Providers;

public class ProviderConfiguration : IProviderConfiguration
{
    public string DefaultProvider { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public Dictionary<string, bool> EnableTools { get; init; } = [];

    public bool EnableCache { get; init; } = true;

    public int CacheTimeoutInSeconds { get; init; } = 3600;

    public int HttpTimeoutInSeconds { get; init; } = 600;

    public List<string> Models { get; init; } = [];
}
