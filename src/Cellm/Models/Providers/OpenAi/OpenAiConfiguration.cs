namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://api.openai.com/v1");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;
}
