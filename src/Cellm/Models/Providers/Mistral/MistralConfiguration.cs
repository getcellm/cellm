
namespace Cellm.Models.Providers.Mistral;

internal class MistralConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Mistral; }

    public string Name { get => "Mistral"; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Mistral)}"; }

    public Uri BaseAddress => new("https://api.mistral.ai/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}