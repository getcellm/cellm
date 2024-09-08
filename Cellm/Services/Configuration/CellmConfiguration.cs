namespace Cellm.Services.Configuration;

public class CellmConfiguration
{
    public string DefaultModelProvider { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public int MaxTokens { get; init; }

    public int CacheTimeoutInSeconds { get; init; }
}

