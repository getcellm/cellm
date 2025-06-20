using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Cellm.Models.Behaviors;

internal class TokenUsageBehavior<TRequest, TResponse>(
    IPublisher publisher,
    ILogger<TokenUsageBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IGetProvider
    where TResponse : IChatResponse
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Let the rest of the pipeline (including the actual handler) run
        var response = await next().ConfigureAwait(false);

        stopwatch.Stop();
        var elapsedTime = stopwatch.Elapsed;

        if (response.ChatResponse?.Usage is null)
        {
            logger.LogDebug(
                "{provider} completed request in {ElapsedMilliseconds:F2}ms. No token usage details found in response.",
                request.Provider,
                elapsedTime.TotalMilliseconds
            );

            return response;
        }

        var requestType = typeof(TRequest).Name;

        logger.LogInformation(
            "{provider} completed request in {ElapsedMilliseconds:F2}ms",
            requestType,
            elapsedTime.TotalMilliseconds
        );

        var notification = new TokenUsageNotification(
            Usage: response.ChatResponse.Usage,
            Provider: request.Provider,
            Model: response.ChatResponse.ModelId,
            ElapsedTime: elapsedTime
        );

        await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
