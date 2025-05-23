namespace Cellm.Models.Resilience;

public class RetryConfiguration
{
    public int MaxRetryAttempts { get; init; } = 3;

    public int DelayInSeconds { get; init; } = 9;

    public int HttpTimeoutInSeconds { get; init; } = 600;  // Must accommodate slow local models
}
