namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}
