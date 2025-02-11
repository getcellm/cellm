using System.ClientModel;
using System.ClientModel.Primitives;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler(HttpClient httpClient)
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = CreateChatClient(
            request.BaseAddress,
            request.Prompt.Options.ModelId ?? string.Empty,
            request.ApiKey);

        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiCompatibleResponse(prompt, chatCompletion);
    }

    public IChatClient CreateChatClient(Uri BaseAddress, string modelId, string apiKey)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient),
                Endpoint = BaseAddress
            });

        return new ChatClientBuilder(openAiClient.AsChatClient(modelId))
            .UseFunctionInvocation()
            .Build();
    }
}
