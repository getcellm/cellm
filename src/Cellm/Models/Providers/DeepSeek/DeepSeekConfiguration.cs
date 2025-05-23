
namespace Cellm.Models.Providers.DeepSeek;

internal class DeepSeekConfiguration : IProviderConfiguration
{
    public Uri BaseAddress => new("https://api.deepseek.com/v1");

    public string DefaultModel { get; init; } = "deepseek-chat";

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = "deepseek-chat";

    public string LargeModel { get; init; } = "deepseek-reasoner";
}
