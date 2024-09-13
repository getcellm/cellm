namespace Cellm.Services.Configuration;

public class RateLimiterConfiguration
{
    public int TokenLimit { get; init; }

    public int QueueLimit { get; init; }

    public int ReplenishmentPeriodInSeconds { get; init; }

    public int TokensPerPeriod { get; init; }
}
