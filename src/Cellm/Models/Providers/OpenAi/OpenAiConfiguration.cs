using Cellm.Users;

namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.OpenAi; }

    public string Name { get => "OpenAI"; }

    public Entitlement Entitlement { get => Entitlement.EnableOpenAiProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.OpenAi)}.png"; }

    public Uri BaseAddress => new("https://api.openai.com/v1");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}
