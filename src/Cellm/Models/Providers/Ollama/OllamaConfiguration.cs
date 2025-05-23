using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://127.0.0.1:11434/");

    public string DefaultModel { get; init; } = "gemma3:4b-it-qat";

    public string SmallModel { get; init; } = "gemma3:4b-it-qat";

    public string MediumModel { get; init; } = "gemma3:12b-it-qat";

    public string LargeModel { get; init; } = "gemma3:27b-it-qat";

    public int MaxInputTokens { get; init; } = 16364;

    public AdditionalPropertiesDictionary AdditionalProperties { get; init; } = [];
}