namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.OpenAiCompatible; }

    public string Name { get => "OpenAI-compatible API"; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.OpenAi)}"; }

    public Uri BaseAddress { get; set; } = new Uri("https://api.openai.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public int HttpTimeoutInSeconds = 3600;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}
