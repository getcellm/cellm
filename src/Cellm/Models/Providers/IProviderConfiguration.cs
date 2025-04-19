namespace Cellm.Models.Providers;

public interface IProviderConfiguration
{
    string DefaultModel { get; init; }

    List<string> Models { get; init; }
}
