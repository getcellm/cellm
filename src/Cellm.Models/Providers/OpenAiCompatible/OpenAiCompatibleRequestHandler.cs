
using System.ClientModel;
using System.ClientModel.Primitives;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Cellm.Models.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler(HttpClient httpClient, IOptions<OpenAiCompatibleConfiguration> openAiCompatibleConfiguration)
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{
    private readonly OpenAiCompatibleConfiguration _openAiCompatibleConfiguration = openAiCompatibleConfiguration.Value;

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = CreateChatClient(request.BaseAddress);

        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiCompatibleResponse(prompt);
    }

    private IChatClient CreateChatClient(Uri? baseAddress)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(_openAiCompatibleConfiguration.ApiKey),
            new OpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient),
                Endpoint = baseAddress ?? _openAiCompatibleConfiguration.BaseAddress
            });

        return new ChatClientBuilder(openAiClient.AsChatClient(_openAiCompatibleConfiguration.DefaultModel))
            .UseFunctionInvocation()
            .Build();
    }
}
