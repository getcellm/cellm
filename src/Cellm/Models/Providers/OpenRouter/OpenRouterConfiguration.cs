using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenRouter;

internal class OpenRouterConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.OpenRouter; }

    public string Name { get => "OpenRouter"; }

    public Entitlement Entitlement { get => Entitlement.EnableOpenRouterProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.OpenRouter)}.svg"; }

    public Uri BaseAddress => new("https://openrouter.ai/api/v1");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool SupportsJsonSchemaResponses { get; init; } = true;

    public bool SupportsStructuredOutputWithTools { get; init; } = true;

    public bool IsEnabled { get; init; } = false;
}
