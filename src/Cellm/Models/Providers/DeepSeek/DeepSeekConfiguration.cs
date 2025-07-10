
namespace Cellm.Models.Providers.DeepSeek;

internal class DeepSeekConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.DeepSeek; }

    public string Name { get => "DeepSeek"; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.DeepSeek)}"; }

    public Uri BaseAddress => new("https://api.deepseek.com/v1");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}
