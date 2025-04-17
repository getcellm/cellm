namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicConfiguration : IProviderConfiguration
{
    public string DefaultModel { get; init; }

    public string Version { get; init; }

    public string ApiKey { get; init; }

    public List<string> Models { get; init; } = [];

    public AnthropicConfiguration()
    {
        DefaultModel = default!;
        Version = default!;
        ApiKey = default!;
    }
}