
namespace Cellm.Models.Providers.Mistral;

internal class MistralConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}