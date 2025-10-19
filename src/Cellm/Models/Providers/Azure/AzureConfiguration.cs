using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Azure;

internal class AzureConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Azure; }

    public string Name { get => "Azure"; }

    public Entitlement Entitlement { get => Entitlement.EnableAzureProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Azure)}.png"; }

    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool SupportsJsonSchemaResponses { get; init; } = true;

    public bool SupportsStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = false;
}
