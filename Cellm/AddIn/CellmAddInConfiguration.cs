namespace Cellm.AddIn;

public class CellmAddInConfiguration
{
    public string DefaultModelProvider { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public int MaxTokens { get; init; }

    public int CacheTimeoutInSeconds { get; init; }

    public RetryConfiguration RetryConfiguration { get; init; } = new();
}

public class RetryConfiguration
{
    public int MaxRetryAttempts { get; init; }

    public int DelayInSeconds { get; init; }

    public int RequestTimeoutInSeconds { get; init; }
}
