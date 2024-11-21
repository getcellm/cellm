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
        var modelId = request.Prompt.Options.ModelId ?? _openAiConfiguration.DefaultModel;

        const string path = "/v1/chat/completions";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        // Must instantiate manually because address can be set/changed only at instantiation
        var chatClient = GetChatClient(address, modelId);
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

        return new ChatClientBuilder()
            .UseLogging()
            .UseFunctionInvocation()
            .Use(openAiClient.AsChatClient(modelId));
    }
}
