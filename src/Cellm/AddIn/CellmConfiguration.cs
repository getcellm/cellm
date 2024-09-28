namespace Cellm.AddIn;

public class CellmConfiguration
{
    public bool Debug { get; init; }

    public string DefaultProvider { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public int MaxTokens { get; init; }

    public int CacheTimeoutInSeconds { get; init; }
}

