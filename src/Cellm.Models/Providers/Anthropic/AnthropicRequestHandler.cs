using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicRequestHandler(
    [FromKeyedServices(Provider.Anthropic)] IChatClient chatClient)
    : IModelRequestHandler<AnthropicRequest, AnthropicResponse>
{
    public async Task<AnthropicResponse> Handle(AnthropicRequest request, CancellationToken cancellationToken)
    {
        var chatResponse = await chatClient.GetResponseAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatResponse.Messages)
            .Build();

        return new AnthropicResponse(prompt, chatResponse);
    }
}
