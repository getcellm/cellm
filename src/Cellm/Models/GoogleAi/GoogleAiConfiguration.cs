using Cellm.Services.Configuration;

namespace Cellm.Models.GoogleAi;

internal class GoogleAiConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public string ApiKey { get; init; }

    public GoogleAiConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
        ApiKey = default!;
    }
}