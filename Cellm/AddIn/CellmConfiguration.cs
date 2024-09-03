namespace Cellm.AddIn;

public class CellmConfiguration
{
    public string DefaultModelProvider { get; init; }

    public double DefaultTemperature { get; init; }

    public int MaxTokens { get; init; }

    public int CacheTimeoutInSeconds { get; init; }

    public CellmConfiguration()
    {
        DefaultModelProvider = string.Empty;
        DefaultTemperature = default;
        MaxTokens = default;
        CacheTimeoutInSeconds = default;
    }
}
