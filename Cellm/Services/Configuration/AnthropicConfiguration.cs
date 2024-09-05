namespace Cellm.Services.Configuration;

internal class AnthropicConfiguration
{
    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public string Version { get; init; }

    public string ApiKey { get; init; }

    public AnthropicConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
        Version = default!;
        ApiKey = default!;
    }
}