namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Anthropic; }

    public string Name { get => "Anthropic"; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Anthropic)}"; }

    public Uri BaseAddress => new("https://api.anthropic.com/v1");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}