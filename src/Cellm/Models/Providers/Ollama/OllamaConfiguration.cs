using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://127.0.0.1:11434/");

    public string DefaultModel { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];

    public int MaxInputTokens { get; init; } = 16364;

    public AdditionalPropertiesDictionary AdditionalProperties { get; init; } = [];
}