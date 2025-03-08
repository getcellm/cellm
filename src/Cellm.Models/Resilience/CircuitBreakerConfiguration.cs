namespace Cellm.Models.Resilience;

internal class CircuitBreakerConfiguration
{
    public int SamplingDurationInSeconds { get; init; }

    public double FailureRatio { get; init; }

    public int MinimumThroughput { get; init; }

    public int BreakDurationInSeconds { get; init; }
}