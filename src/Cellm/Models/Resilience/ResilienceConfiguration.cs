namespace Cellm.Models.Resilience;

internal class ResilienceConfiguration
{
    public RateLimiterConfiguration RateLimiterConfiguration { get; init; } = new();

    public RetryConfiguration RetryConfiguration { get; init; } = new();
}