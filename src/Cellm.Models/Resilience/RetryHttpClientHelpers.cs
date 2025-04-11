using System.Net;
using Anthropic.SDK;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Cellm.Models.Resilience;

internal static class RetryHttpClientHelpers
{
    public static bool ShouldBreakCircuit(Outcome<HttpResponseMessage> outcome) => outcome switch
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

    public static bool ShouldRetry(Outcome<HttpResponseMessage> outcome) => outcome switch
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
        exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or RateLimitsExceeded;
}

