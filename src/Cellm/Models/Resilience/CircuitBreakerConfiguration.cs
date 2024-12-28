namespace Cellm.Models.Resilience;

public class CircuitBreakerConfiguration
{
    public int SamplingDurationInSeconds { get; init; }

    public double FailureRatio { get; init; }

    public int MinimumThroughput { get; init; }

    public int BreakDurationInSeconds { get; init; }
}