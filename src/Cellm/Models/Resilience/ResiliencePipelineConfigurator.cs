using System.Net;
using System.Threading.RateLimiting;
using Cellm.Models.Providers;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Cellm.Models.Resilience;

public class ResiliencePipelineConfigurator
{
    private readonly ProviderConfiguration _providerConfiguration;
    private readonly RateLimiterConfiguration _rateLimiterConfiguration;
    private readonly CircuitBreakerConfiguration _circuitBreakerConfiguration;
    private readonly RetryConfiguration _retryConfiguration;

    public ResiliencePipelineConfigurator(IConfiguration configuration)
    {
        _providerConfiguration = configuration.GetRequiredSection(nameof(ProviderConfiguration)).Get<ProviderConfiguration>()
            ?? throw new NullReferenceException(nameof(ProviderConfiguration));

        _rateLimiterConfiguration = configuration.GetRequiredSection(nameof(RateLimiterConfiguration)).Get<RateLimiterConfiguration>()
            ?? throw new NullReferenceException(nameof(RateLimiterConfiguration));

        _circuitBreakerConfiguration = configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)).Get<CircuitBreakerConfiguration>()
            ?? throw new NullReferenceException(nameof(CircuitBreakerConfiguration));

        _retryConfiguration = configuration.GetRequiredSection(nameof(RetryConfiguration)).Get<RetryConfiguration>()
            ?? throw new NullReferenceException(nameof(RetryConfiguration));
    }

    public void ConfigureResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        _ = builder
            .AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = _rateLimiterConfiguration.TokenLimit,
                QueueLimit = _rateLimiterConfiguration.QueueLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(_rateLimiterConfiguration.ReplenishmentPeriodInSeconds),
                TokensPerPeriod = _rateLimiterConfiguration.TokensPerPeriod,
            }))
            .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = _rateLimiterConfiguration.ConcurrencyLimit,
                QueueLimit = _rateLimiterConfiguration.QueueLimit
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome)),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = _retryConfiguration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_retryConfiguration.DelayInSeconds),
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldBreakCircuit(args.Outcome)),
                FailureRatio = _circuitBreakerConfiguration.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(_circuitBreakerConfiguration.SamplingDurationInSeconds),
                MinimumThroughput = _circuitBreakerConfiguration.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(_circuitBreakerConfiguration.BreakDurationInSeconds),
            })
            .AddTimeout(TimeSpan.FromSeconds(_providerConfiguration.HttpTimeoutInSeconds))
            .Build();
    }

    private static bool ShouldBreakCircuit(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: HttpResponseMessage response } => IsCircuitBreakerError(response),
        { Exception: Exception exception } => IsCircuitBreakerException(exception),
        _ => false
    };

    private static bool IsCircuitBreakerError(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.RequestTimeout ||
        response.StatusCode == HttpStatusCode.TooManyRequests ||
        response.StatusCode == HttpStatusCode.GatewayTimeout;

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
        response.StatusCode == HttpStatusCode.RequestTimeout ||
        response.StatusCode == HttpStatusCode.TooManyRequests ||
        response.StatusCode == HttpStatusCode.BadGateway ||
        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
        response.StatusCode == HttpStatusCode.GatewayTimeout;


    private static bool IsRetryableException(Exception exception) =>
        exception is HttpRequestException or TimeoutRejectedException;
}

