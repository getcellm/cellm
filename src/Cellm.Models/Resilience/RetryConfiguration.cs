namespace Cellm.Services.Configuration;

public class RetryConfiguration
{
    public int MaxRetryAttempts { get; init; }

    public int DelayInSeconds { get; init; }
}
