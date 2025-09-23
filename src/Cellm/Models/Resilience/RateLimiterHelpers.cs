using System.ClientModel;
using Anthropic.SDK;
using Cellm.Models.Prompts;
using Polly;
using Polly.Timeout;

namespace Cellm.Models.Resilience;

internal static class RateLimiterHelpers
{
    private static readonly List<int> retryableStatusCodes = [
        428,
        429,
        500,
        501,
        503
    ];

    public static bool ShouldRetry(Outcome<Prompt> outcome) => outcome switch
    {
        { Result: Prompt prompt } => IsRetryableError(prompt),
        { Exception: Exception exception } => IsRetryableException(exception),
        _ => false
    };

    private static bool IsRetryableError(Prompt prompt) => false;

    private static bool IsRetryableException(Exception exception) => exception switch
    {
        ClientResultException clientResultException => retryableStatusCodes.Contains(clientResultException.Status),
        RateLimitsExceeded => true,
        _ => false
    };
}
