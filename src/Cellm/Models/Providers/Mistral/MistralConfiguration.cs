
namespace Cellm.Models.Providers.Mistral;

internal class MistralConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.mistral.ai/v1/");

    public string DefaultModel { get; init; } = "mistral-small-latest";

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = "mistral-small-latest";

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = "mistral-large-latest";
}