using Cellm.Tools.FileReader;
using Cellm.Tools.FileSearch;

namespace Cellm.Models.Providers;

public class ProviderConfiguration
{
    public string DefaultProvider { get; init; } = nameof(Provider.Ollama);

    public string DefaultModel { get; init; } = "gemma3:4b-it-qat";

    public double DefaultTemperature { get; init; } = 0;

    public int MaxOutputTokens { get; init; } = 8192;

    public Dictionary<string, bool> EnableTools { get; init; } = new() 
    {
        {nameof(FileSearchRequest), false},
        {nameof(FileReaderRequest), false}
    };

    public Dictionary<string, bool> EnableModelContextProtocolServers { get; init; } = new()
    {
        {"Playwright", false},
    };

    public bool EnableCache { get; init; } = true;

    public int CacheTimeoutInSeconds { get; init; } = 3600;
}
