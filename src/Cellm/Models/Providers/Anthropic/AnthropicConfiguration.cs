namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicConfiguration : IProviderConfiguration
{
    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;
}