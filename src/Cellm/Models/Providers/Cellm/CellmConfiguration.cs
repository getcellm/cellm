namespace Cellm.Models.Providers.Cellm;

internal class CellmConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://getcellm.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;
}
