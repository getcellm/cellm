using Cellm.AddIn;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Cellm.Models.Behaviors;

internal class SentryBehavior<TRequest, TResponse>(ILogger<SentryBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!SentrySdk.IsEnabled)
        {
            logger.LogDebug("Sentry disabled");
            return await next();
        }

        logger.LogDebug("Sentry enabled");

        var transaction = SentrySdk.StartTransaction($"{nameof(Cellm)}.{nameof(Models)}", typeof(TRequest).Name);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        try
        {
            transaction.Contexts["ChatOptions"] = request.Prompt.Options;
            transaction.Contexts["UserInstructions"] = GetUserInstructions(request.Prompt.Messages);

            return await next();
        }
        finally
        {            
            transaction.Finish();
        }
    }

    private static string GetUserInstructions(IList<ChatMessage> messages)
    {
        var userMessage = messages
            .Where(x => x.Role == ChatRole.User)
            .First()
            .Text;

        var startIndex = userMessage.IndexOf(ArgumentParser.InstructionsStartTag, StringComparison.OrdinalIgnoreCase) + ArgumentParser.InstructionsStartTag.Length;

        if (startIndex < 0)
        {
            return string.Empty;
        }

        var endIndex = userMessage.IndexOf(ArgumentParser.InstructionsEndTag, startIndex, StringComparison.OrdinalIgnoreCase);

        if (endIndex < 0)
        {
            return string.Empty;
        }

        return userMessage[startIndex..endIndex];
    }
}
