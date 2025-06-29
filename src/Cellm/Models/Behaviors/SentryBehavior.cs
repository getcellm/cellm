﻿using Cellm.AddIn;
using Cellm.Users;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Behaviors;

internal class SentryBehavior<TRequest, TResponse>(
    IOptionsMonitor<CellmAddInConfiguration> providerConfiguration,
    Account account,
    ILogger<SentryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IGetPrompt
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var disableTelemetry = await account.HasEntitlementAsync(Entitlement.DisableTelemetry);

        if (!SentrySdk.IsEnabled || disableTelemetry)
        {
            logger.LogDebug("Telemetry disabled");
            return await next().ConfigureAwait(false);
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
            return await next().ConfigureAwait(false);
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
