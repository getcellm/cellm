using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler(IChatClientFactory chatClientFactory)
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = chatClientFactory.GetClient(request.Provider);

        var chatResponse = await chatClient.GetResponseAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new OpenAiCompatibleResponse(prompt, chatResponse);
    }
}
