using Polly;
using Cellm.Models.Prompts;
using Polly.Timeout;
using Anthropic.SDK;
using System.ClientModel;

namespace Cellm.Models.Resilience;

internal static class RateLimiterHelpers
{
    private static readonly List<int> retryableStatusCodes = [428, 429, 500, 501, 502, 503, 504];

    public static bool ShouldRetry(Outcome<Prompt> outcome) => outcome switch
    {
        { Result: Prompt prompt } => IsRetryableError(prompt),
        { Exception: Exception exception } => IsRetryableException(exception),
        _ => false
    };

    private static bool IsRetryableError(Prompt prompt) => false;


    private static bool IsRetryableException(Exception exception) => exception switch
    {
        TimeoutRejectedException => true,
        ClientResultException clientResultException => retryableStatusCodes.Contains(clientResultException.Status),
        RateLimitsExceeded => true,
        _ => false
    };
}
