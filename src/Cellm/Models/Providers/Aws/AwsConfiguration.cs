using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Aws;

internal class AwsConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Aws; }

    public string Name { get => "AWS"; }

    public Entitlement Entitlement { get => Entitlement.EnableAwsProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Aws)}.png"; }

    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool CanUseStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = false;
}
