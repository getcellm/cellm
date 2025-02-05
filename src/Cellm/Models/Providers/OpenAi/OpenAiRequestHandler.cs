using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiRequestHandler(
    IOptionsMonitor<OpenAiConfiguration> openAiConfiguration) : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
{

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {
        var defaultModel = openAiConfiguration.CurrentValue.DefaultModel;
        var chatClient = CreateChatClient(request.Prompt.Options.ModelId ?? defaultModel, openAiConfiguration.CurrentValue.ApiKey);
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiResponse(prompt);
    }

    public IChatClient CreateChatClient(string modelId, string apiKey)
    {
        return new ChatClientBuilder(new OpenAIClient(apiKey).AsChatClient(modelId))
            .UseFunctionInvocation()
            .Build();
    }
}
