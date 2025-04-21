namespace Cellm.Models.Providers;

public interface IProviderConfiguration
{
    string DefaultModel { get; init; }

    string SmallModel { get; init; }

    string BigModel { get; init; }

    string ThinkingModel { get; init; }
}
