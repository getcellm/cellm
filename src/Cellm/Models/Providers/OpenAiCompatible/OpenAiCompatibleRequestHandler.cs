using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler(
    OpenAiCompatibleChatClientFactory openAiCompatibleChatClientFactory,
    IOptions<OpenAiCompatibleConfiguration> openAiCompatibleConfiguration)
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = openAiCompatibleChatClientFactory.Create(
            request.BaseAddress,
            request.Prompt.Options.ModelId ?? string.Empty,
            request.ApiKey ?? "API_KEY");

        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiCompatibleResponse(prompt);
    }
}
