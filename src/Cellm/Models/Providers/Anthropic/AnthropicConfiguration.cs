namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://api.anthropic.com/v1");

    public string DefaultModel { get; init; } = "claude-3-7-sonnet-latest";

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = "claude-3-5-haiku-latest";

    public string MediumModel { get; init; } = "claude-3-7-sonnet-latest";

    public string LargeModel { get; init; } = "claude-sonnet-4-20250514";
}