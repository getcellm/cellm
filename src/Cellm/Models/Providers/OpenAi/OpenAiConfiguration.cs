namespace Cellm.Models.Providers.OpenAi;

public class OpenAiConfiguration : IProviderConfiguration
{
    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;
}
