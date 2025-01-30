namespace Cellm.Models.Providers.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri ZipUrl { get; init; } = default!;

    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}