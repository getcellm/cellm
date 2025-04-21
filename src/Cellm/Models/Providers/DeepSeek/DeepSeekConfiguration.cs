
namespace Cellm.Models.Providers.DeepSeek;

internal class DeepSeekConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.deepseek.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string BigModel { get; init; } = string.Empty;

    public string ThinkingModel { get; init; } = string.Empty;
}
