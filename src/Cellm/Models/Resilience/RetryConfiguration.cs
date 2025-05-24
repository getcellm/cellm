namespace Cellm.Models.Resilience;

public class RetryConfiguration
{
    public int MaxRetryAttempts { get; init; }

    public int DelayInSeconds { get; init; }

    public int HttpTimeoutInSeconds { get; init; }
}
