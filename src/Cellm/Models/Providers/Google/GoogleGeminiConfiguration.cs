namespace Cellm.Models.Providers.Google;

internal class GoogleGeminiConfiguration
{
    public Uri BaseAddress => new("https://generativelanguage.googleapis.com/v1beta/openai/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;
}
