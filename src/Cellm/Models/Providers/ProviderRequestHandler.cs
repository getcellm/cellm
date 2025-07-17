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
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            StructuredOutputShape.Range =>
                await chatClient.GetResponseAsync<string[][]>(
                    request.Prompt.Messages,
                    request.Prompt.Options,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            _ => throw new CellmException($"Internal error: Unknown output shape ({request.Prompt.OutputShape})")
        };

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new ProviderResponse(prompt, chatResponse);
    }
}

