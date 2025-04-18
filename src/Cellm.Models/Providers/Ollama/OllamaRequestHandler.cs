using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaRequestHandler(
    [FromKeyedServices(Provider.Ollama)] IChatClient chatClient) : IModelRequestHandler<OllamaRequest, OllamaResponse>
{
    public async Task<OllamaResponse> Handle(OllamaRequest request, CancellationToken cancellationToken)
    {
        if (request.Prompt.Options.AdditionalProperties is null)
        {
            request.Prompt.Options.AdditionalProperties = [];
        }

        request.Prompt.Options.AdditionalProperties["num_ctx"] = 8192;

        var chatResponse = await chatClient.GetResponseAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new OllamaResponse(prompt, chatResponse);
    }
}
