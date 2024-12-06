using Cellm.Services.Configuration;

namespace Cellm.Models.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri ZipUrl { get; init; } = default!;

    public Uri BaseAddress { get; init; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public bool EnableServer { get; init; } = false;
}