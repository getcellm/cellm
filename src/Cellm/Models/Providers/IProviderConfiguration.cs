namespace Cellm.Models.Providers;

public interface IProviderConfiguration
{
    string DefaultModel { get; init; }

    string SmallModel { get; init; }

    string MediumModel { get; init; }

    string LargeModel { get; init; }
}
