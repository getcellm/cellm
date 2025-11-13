using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.OpenAiCompatible; }

    public string Name { get => "OpenAI-compatible API"; }

    public Entitlement Entitlement { get => Entitlement.EnableOpenAiCompatibleProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.OpenAi)}.png"; }

    public Uri BaseAddress { get; init; } = new Uri("https://api.openai.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public int HttpTimeoutInSeconds = 3600;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool SupportsJsonSchemaResponses { get; init; } = false;

    public bool SupportsStructuredOutputWithTools { get; init; } = true;

    public bool IsEnabled { get; init; } = false;
}
