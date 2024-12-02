using Cellm.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.OpenAi;

internal class OpenAiRequestHandler([FromKeyedServices(Providers.OpenAi)] IChatClient chatClient) : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
{

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiResponse(prompt);
    }
}
