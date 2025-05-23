using Cellm.AddIn;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Users;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class SentryBehavior<TRequest, TResponse>(
    IOptionsMonitor<ProviderConfiguration> providerConfiguration,
    Account account,
    ILogger<SentryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IPrompt
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var disableTelemetry = await account.HasEntitlementAsync(Entitlement.DisableTelemetry);

        if (!SentrySdk.IsEnabled || disableTelemetry)
        {
            logger.LogDebug("Telemetry disabled");
            return await next();
        }

        logger.LogDebug("Telemetry enabled");

        var transaction = SentrySdk.StartTransaction($"{nameof(Cellm)}.{nameof(Models)}.{nameof(Client)}", typeof(TRequest).Name);

        transaction.Contexts["Prompt"] = new
        {
            Instructions = GetInstructions(request.Prompt.Messages),
            Tools = providerConfiguration.CurrentValue.EnableTools,
            Servers = providerConfiguration.CurrentValue.EnableModelContextProtocolServers,
            request.Prompt.Options,
        };

        try
        {
            return await next();
        }
        finally
        {
            transaction.Finish();
        }
    }

    private static string GetInstructions(IList<ChatMessage> messages)
    {
        var userMessage = messages
            .Where(x => x.Role == ChatRole.User)
            .First()
            .Text;

        var startIndex = userMessage.IndexOf(ArgumentParser.InstructionsBeginTag, StringComparison.OrdinalIgnoreCase) + ArgumentParser.InstructionsBeginTag.Length;

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
