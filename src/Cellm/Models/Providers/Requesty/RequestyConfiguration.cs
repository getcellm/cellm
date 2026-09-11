using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Requesty;

internal class RequestyConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Requesty; }

    public string Name { get => "Requesty"; }

    public Entitlement Entitlement { get => Entitlement.EnableRequestyProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Requesty)}.svg"; }

    public Uri BaseAddress => new("https://router.requesty.ai/v1");

    public string DefaultModel { get; init; } = "openai/gpt-4o-mini";

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool SupportsJsonSchemaResponses { get; init; } = true;

    public bool SupportsStructuredOutputWithTools { get; init; } = true;

    public bool IsEnabled { get; init; } = false;
}
