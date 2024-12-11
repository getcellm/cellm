namespace Cellm.Models.Providers.Ollama;

public class OllamaConfiguration : IProviderConfiguration
{
    public Uri ZipUrl { get; init; } = default!;

    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public bool EnableServer { get; init; } = false;
}