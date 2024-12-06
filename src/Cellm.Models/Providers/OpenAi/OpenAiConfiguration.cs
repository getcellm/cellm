using Cellm.Services.Configuration;

namespace Cellm.Models.OpenAi;

internal class OpenAiConfiguration : IProviderConfiguration
{
    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;
}
