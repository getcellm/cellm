using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaRequestHandler(
    [FromKeyedServices(Provider.Ollama)] IChatClient chatClient) : IModelRequestHandler<OllamaRequest, OllamaResponse>
{
    public async Task<OllamaResponse> Handle(OllamaRequest request, CancellationToken cancellationToken)
    {
        var chatCompletion = await chatClient.CompleteAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OllamaResponse(prompt);
    }
}
