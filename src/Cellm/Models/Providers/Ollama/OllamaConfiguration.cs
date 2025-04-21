using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://127.0.0.1:11434/");

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;

    public int MaxInputTokens { get; init; } = 16364;

    public AdditionalPropertiesDictionary AdditionalProperties { get; init; } = [];
}