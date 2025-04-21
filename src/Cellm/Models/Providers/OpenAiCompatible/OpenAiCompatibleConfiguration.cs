namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public int HttpTimeoutInSeconds = 3600;

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;
}
