using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Cellm.Models.Behaviors;

internal class UsageBehavior<TRequest, TResponse>(
    IPublisher publisher,
    ILogger<UsageBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IGetProvider
    where TResponse : IChatResponse, IGetPrompt
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Let the rest of the pipeline (including the actual handler) run
        var response = await next().ConfigureAwait(false);

        stopwatch.Stop();
        var elapsedTime = stopwatch.Elapsed;

        var usageDetails = response.ChatResponse?.Usage;

        if (usageDetails is null)
        {
            logger.LogDebug(
                "No token usage details found in {provider} response. Guesstimating values.",
                request.Provider
            );

            var systemTokenCount = response.Prompt.Messages.Where(x => x.Role == ChatRole.System).Sum(x => x.Text.Length) / 4;
            var userTokenCount = response.Prompt.Messages.Where(x => x.Role == ChatRole.User).Sum(x => x.Text.Length) / 4;
            var assistantTokenCount = response.Prompt.Messages.Where(x => x.Role == ChatRole.Assistant).Sum(x => x.Text.Length) / 4;

            usageDetails = new UsageDetails
            {
                InputTokenCount = systemTokenCount + userTokenCount,
                OutputTokenCount = assistantTokenCount
            };
        }

        logger.LogInformation(
            "{provider} completed request in {ElapsedMilliseconds}ms",
            request.Provider,
            elapsedTime.TotalMilliseconds
        );

        var notification = new UsageNotification(
            Usage: usageDetails,
            Provider: request.Provider,
            Model: response.ChatResponse?.ModelId,
            ElapsedTime: elapsedTime
        );

        await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
