namespace Cellm.Models.Resilience;

internal class CircuitBreakerConfiguration
{
    public int SamplingDurationInSeconds { get; init; } = 30;

    public double FailureRatio { get; init; } = 0.3;

    public int MinimumThroughput { get; init; } = 30;

    public int BreakDurationInSeconds { get; init; } = 8;
}