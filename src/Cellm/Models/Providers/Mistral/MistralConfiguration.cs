
namespace Cellm.Models.Providers.Mistral;

internal class MistralConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.mistral.ai/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;
}