namespace Cellm.AddIn;

public class CellmAddInConfiguration
{
    public string DefaultProvider { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public int MaxOutputTokens { get; init; } = 8192;

    public Dictionary<string, bool> EnableTools { get; init; } = [];

    public Dictionary<string, bool> EnableModelContextProtocolServers { get; init; } = [];

    public bool EnableCache { get; init; } = true;

    public int CacheTimeoutInSeconds { get; init; } = 3600;

    public List<string> Models { get; init; } = [];
}
