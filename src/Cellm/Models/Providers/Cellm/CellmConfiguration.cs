namespace Cellm.Models.Providers.Cellm;

internal class CellmConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.getcellm.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;
}
