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
                    request.Prompt.Messages,
                    request.Prompt.Options,
                    UseJsonSchemaResponseFormat(request.Provider, request.Prompt),
                    cancellationToken).ConfigureAwait(false),
            StructuredOutputShape.Range =>
                await chatClient.GetResponseAsync<string[][]>(
                    request.Prompt.Messages,
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

    // Determines if we should impose the JSON schema on the response or fallback
    // to appending a JSON schema to the user message.
    private static bool UseJsonSchemaResponseFormat(Provider provider, Prompt prompt)
    {
        var providerConfiguration = CellmAddIn.GetProviderConfiguration(provider);

        if (!providerConfiguration.SupportsStructuredOutput)
        {
            return false;
        }

        // Provider can interleave JSON schema responses and tool responses 
        // OR provider supports JSON schema responses iff tools are disabled
        var isToolsEnabled = prompt.Options.Tools?.Any() ?? false;
        return providerConfiguration.SupportsStructuredOutputWithTools || !isToolsEnabled;
    }
}
