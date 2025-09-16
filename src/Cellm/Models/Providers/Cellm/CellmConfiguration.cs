using Cellm.Users;

namespace Cellm.Models.Providers.Cellm;

internal class CellmConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Cellm; }

    public string Name { get => "Cellm"; }

    public Entitlement Entitlement { get => Entitlement.EnableCellmProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Cellm)}.svg"; }

    public Uri BaseAddress { get; init; } = new("https://www.getcellm.com/v1/");

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool CanUseStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = true;
}
