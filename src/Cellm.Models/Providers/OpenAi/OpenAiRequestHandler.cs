using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiRequestHandler([FromKeyedServices(Provider.OpenAi)] IChatClient chatClient) 
    : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
{

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiResponse(prompt, chatCompletion);
    }
}
