using Cellm.Services.Configuration;

namespace Cellm.Models.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public string ApiKey { get; init; }

    public OpenAiConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
        ApiKey = default!;
    }
}
