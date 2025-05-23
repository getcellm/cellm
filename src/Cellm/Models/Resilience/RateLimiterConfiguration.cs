namespace Cellm.Models.Resilience;

internal class RateLimiterConfiguration
{
    public int QueueLimit { get; init; } = 1048576;  // Max number of Excel rows

    public int TokenLimit { get; init; } = 2;

    public int ReplenishmentPeriodInSeconds { get; init; } = 1;

    public int TokensPerPeriod { get; init; } = 2;

    public int ConcurrencyLimit { get; init; } = 4;
}
