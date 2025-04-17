namespace Cellm.Models.Providers.Llamafile;

internal class LlamafileConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("http://127.0.0.1:8080/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public List<string> Models { get; init; } = [];
}
