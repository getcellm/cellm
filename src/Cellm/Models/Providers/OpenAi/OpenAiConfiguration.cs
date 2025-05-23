namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://api.openai.com/v1");

    public string DefaultModel { get; init; } = "gpt-4.1-mini";

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = "gpt-4.1-mini";

    public string MediumModel { get; init; } = "gpt-4.1";

    public string LargeModel { get; init; } = "o4-mini";
}
