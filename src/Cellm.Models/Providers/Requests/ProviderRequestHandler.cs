using Cellm.Models.Prompts;
using MediatR;

namespace Cellm.Models.Providers;

internal class ProviderRequestHandler(IChatClientFactory chatClientFactory) : IRequestHandler<ProviderRequest, ProviderResponse>
{

    public async Task<ProviderResponse> Handle(ProviderRequest request, CancellationToken cancellationToken)
    {
        var chatClient = chatClientFactory.GetClient(request.Provider);

        var chatResponse = await chatClient.GetResponseAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new ProviderResponse(prompt, chatResponse);
    }
}

