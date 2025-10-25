using System.Text;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Prompts;
using MediatR;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers;

internal class ProviderRequestHandler(IChatClientFactory chatClientFactory) : IRequestHandler<ProviderRequest, ProviderResponse>
{
    public async Task<ProviderResponse> Handle(ProviderRequest request, CancellationToken cancellationToken)
    {
        var chatClient = chatClientFactory.GetClient(request.Provider);

        var chatResponse = request.Prompt.OutputShape switch
        {
            StructuredOutputShape.None =>
                await chatClient.GetResponseAsync(
                    request.Prompt.Messages,
                    request.Prompt.Options,
                    cancellationToken).ConfigureAwait(false),
            StructuredOutputShape.Row or StructuredOutputShape.Column =>
                await chatClient.GetResponseAsync<string[]>(
                    AppendOutputShapeInstructions(request.Prompt.Messages, SystemMessages.RowOrColumn),
                    request.Prompt.Options,
                    UseJsonSchemaResponseFormat(request.Provider, request.Prompt),
                    cancellationToken).ConfigureAwait(false),
            StructuredOutputShape.Range =>
                await chatClient.GetResponseAsync<string[][]>(
                    AppendOutputShapeInstructions(request.Prompt.Messages, SystemMessages.Range),
                    request.Prompt.Options,
                    UseJsonSchemaResponseFormat(request.Provider, request.Prompt),
                    cancellationToken).ConfigureAwait(false),
            _ => throw new CellmException($"Internal error: Unknown output shape ({request.Prompt.OutputShape})")
        };

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new ProviderResponse(prompt, chatResponse);
    }

    private static IList<ChatMessage> AppendOutputShapeInstructions(IList<ChatMessage> messages, string outputShapeInstructions)
    {
        var systemMessage = messages.First(x => x.Role == ChatRole.System);
        var systemMessageWithOutputShapeInstructions = new StringBuilder(systemMessage.Text)
            .AppendLine()
            .AppendLine()
            .Append(outputShapeInstructions)
            .ToString();

        var index = messages.IndexOf(systemMessage);
        messages[index] = new ChatMessage(ChatRole.System, systemMessageWithOutputShapeInstructions);

        return messages;
    }

    // Determines if we should impose the JSON schema on the response, fallback
    // to appending a JSON schema to the user message, or throw a helpful error message
    private static bool UseJsonSchemaResponseFormat(Provider provider, Prompt prompt)
    {
        var providerConfiguration = CellmAddIn.GetProviderConfiguration(provider);
        var supportsJsonSchemaResponses = providerConfiguration.SupportsJsonSchemaResponses;
        var supportsStructuredOutputWithTools = providerConfiguration.SupportsStructuredOutputWithTools;
        var isAnyToolEnabled = prompt.Options.Tools?.Any() ?? false;

        return (supportsJsonSchemaResponses, supportsStructuredOutputWithTools, isAnyToolEnabled) switch
        {
            // Provider can interleave JSON schema responses and tool responses
            (true, true, _) => true,
            // Provider supports JSON schema responses if and only if tools are disabled
            (true, false, true) => throw new CellmException($"{provider} does not support {prompt.OutputShape} output when tools are enabled"),
            (true, false, false) => true,
            // Provider can interleave JSON prompt responses and tool responses
            (false, true, _) => false,
            // Provider supports JSON prompt responses if and only if tools are disabled
            (false, false, true) => throw new CellmException($"{provider} does not support {prompt.OutputShape} output when tools are enabled"),
            (false, false, false) => false,
        };
    }
}
