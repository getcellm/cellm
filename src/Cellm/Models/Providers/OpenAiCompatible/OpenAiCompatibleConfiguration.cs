namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleConfiguration
{
    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public int HttpTimeoutInSeconds = 3600;

    public List<string> Models { get; init; } = [];
}
