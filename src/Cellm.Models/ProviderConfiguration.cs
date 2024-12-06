namespace Cellm.Models;

internal class ProviderConfiguration : IProviderConfiguration
{
    public string DefaultProvider { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }
}
