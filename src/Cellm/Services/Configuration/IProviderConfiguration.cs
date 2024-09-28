namespace Cellm.Services.Configuration;

internal interface IProviderConfiguration
{
    Uri BaseAddress { get; init; }

    string DefaultModel { get; init; }
}
