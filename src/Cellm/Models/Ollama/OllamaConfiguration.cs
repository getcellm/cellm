using Cellm.Services.Configuration;

namespace Cellm.Models.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri OllamaUri { get; init; }

    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public OllamaConfiguration()
    {
        OllamaUri = default!;
        BaseAddress = default!;
        DefaultModel = default!;
    }
}