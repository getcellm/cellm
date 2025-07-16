using Cellm.Users;

namespace Cellm.Models.Providers;

internal interface IProviderConfiguration
{
    Provider Id { get; }

    string Name { get; }

    Entitlement Entitlement { get; }

    string Icon { get; }

    string DefaultModel { get; init; }

    string SmallModel { get; init; }

    string MediumModel { get; init; }

    string LargeModel { get; init; }

    bool CanUseStructuredOutputWithTools { get; init; }

    bool IsEnabled { get; init; }
}
