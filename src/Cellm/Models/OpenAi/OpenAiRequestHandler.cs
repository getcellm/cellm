using System.ClientModel;
using System.ClientModel.Primitives;
using Cellm.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Cellm.Models.OpenAi;

internal class OpenAiRequestHandler(IOptions<OpenAiConfiguration> openAiConfiguration, HttpClient httpClient) : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
{
    private readonly OpenAiConfiguration _openAiConfiguration = openAiConfiguration.Value;

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {
        // Must instantiate manually because address can be set/changed only at instantiation
        var baseAddress = request.BaseAddress is null ? _openAiConfiguration.BaseAddress : request.BaseAddress;
        var modelId = request.Prompt.Options.ModelId ?? _openAiConfiguration.DefaultModel;

        var chatClient = GetChatClient(baseAddress, modelId);
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiResponse(prompt);
    }

    private IChatClient GetChatClient(Uri address, string modelId)
    {
        var openAiClientCredentials = new ApiKeyCredential(_openAiConfiguration.ApiKey);
        var openAiClientOptions = new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(httpClient),
            Endpoint = address
        };

        var openAiClient = new OpenAIClient(openAiClientCredentials, openAiClientOptions);

        return new ChatClientBuilder(openAiClient.AsChatClient(modelId))
            .UseFunctionInvocation()
            .Build();
    }
}
