namespace Cellm.Models.Providers.Cellm;

internal class CellmConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.getcellm.com/v1/");

    public string DefaultModel { get; init; } = "gemini-2.5-flash-preview-05-20";

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = "gemini-2.5-flash-preview-05-20";

    public string LargeModel { get; init; } = "gemini-2.5-pro-preview-05-06";
}
