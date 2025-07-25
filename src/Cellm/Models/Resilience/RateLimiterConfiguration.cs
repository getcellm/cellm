﻿namespace Cellm.Models.Resilience;

internal class RateLimiterConfiguration
{
    public int RateLimiterQueueLimit { get; init; }

    public int TokenLimit { get; init; }

    public int ReplenishmentPeriodInSeconds { get; init; }

    public int TokensPerPeriod { get; init; }

    public int ConcurrencyLimiterQueueLimit { get; init; }

    public int ConcurrencyLimit { get; init; }
}
