using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Cellm.Services.Configuration;


public class ResiliencePipelineConfigurator
{
    private readonly IConfiguration _configuration;

    public ResiliencePipelineConfigurator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        var rateLimiterConfiguration = _configuration.GetRequiredSection(nameof(RateLimiterConfiguration)).Get<RateLimiterConfiguration>()
            ?? throw new NullReferenceException(nameof(RateLimiterConfiguration));

        var circuitBreakerConfiguration = _configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)).Get<CircuitBreakerConfiguration>()
            ?? throw new NullReferenceException(nameof(CircuitBreakerConfiguration));

        var retryConfiguration = _configuration.GetRequiredSection(nameof(RetryConfiguration)).Get<RetryConfiguration>()
            ?? throw new NullReferenceException(nameof(RetryConfiguration));

        _ = builder
            .AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = rateLimiterConfiguration.TokenLimit,
                QueueLimit = rateLimiterConfiguration.QueueLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimiterConfiguration.ReplenishmentPeriodInSeconds),
                TokensPerPeriod = rateLimiterConfiguration.TokensPerPeriod,
            }))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldBreakCircuit(args.Outcome)),
                FailureRatio = circuitBreakerConfiguration.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(circuitBreakerConfiguration.SamplingDurationInSeconds),
                MinimumThroughput = circuitBreakerConfiguration.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(circuitBreakerConfiguration.BreakDurationInSeconds),
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome)),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = retryConfiguration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(retryConfiguration.DelayInSeconds),
            })
            .AddTimeout(TimeSpan.FromSeconds(retryConfiguration.RequestTimeoutInSeconds))
            .Build();
    }

    private static bool ShouldBreakCircuit(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: HttpResponseMessage response } => IsCircuitBreakerError(response),
        { Exception: Exception exception } => IsCircuitBreakerException(exception),
        _ => false
    };

    private static bool IsCircuitBreakerError(HttpResponseMessage response) =>
        response.StatusCode >= HttpStatusCode.InternalServerError ||
        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
        response.StatusCode == HttpStatusCode.TooManyRequests;

    private static bool IsCircuitBreakerException(Exception exception) =>
        IsRetryableException(exception) || IsCatastrophicException(exception);

    private static bool IsCatastrophicException(Exception exception) =>
        exception is OutOfMemoryException or ThreadAbortException;

    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: HttpResponseMessage response } => IsRetryableError(response),
        { Exception: Exception exception } => IsRetryableException(exception),
        _ => false
    };

    private static bool IsRetryableError(HttpResponseMessage response) =>
        response.StatusCode >= HttpStatusCode.InternalServerError ||
        response.StatusCode == HttpStatusCode.RequestTimeout ||
        response.StatusCode == HttpStatusCode.TooManyRequests;

    private static bool IsRetryableException(Exception exception) =>
        exception is HttpRequestException or TimeoutRejectedException;
}

