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
        var api_key = string.IsNullOrEmpty(request.ApiKey) ? "API_KEY" : request.ApiKey;

        var chatClient = openAiCompatibleChatClientFactory.Create(
            request.BaseAddress ?? openAiCompatibleConfiguration.Value.BaseAddress,
            request.Prompt.Options.ModelId ?? string.Empty,
            api_key);

        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiCompatibleResponse(prompt);
    }
}
