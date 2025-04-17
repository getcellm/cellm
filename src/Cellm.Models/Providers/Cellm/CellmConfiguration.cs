namespace Cellm.Models.Providers.Cellm;

internal class CellmConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.getcellm.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}
