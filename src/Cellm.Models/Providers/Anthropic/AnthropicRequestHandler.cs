using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Anthropic;

internal class AnthropicRequestHandler(
    [FromKeyedServices(Provider.Anthropic)] IChatClient chatClient,
    IOptionsMonitor<ProviderConfiguration> providerConfiguration)
    : IModelRequestHandler<AnthropicRequest, AnthropicResponse>
{
    public async Task<AnthropicResponse> Handle(AnthropicRequest request, CancellationToken cancellationToken)
    {
        // Required by Anthropic API
        request.Prompt.Options.MaxOutputTokens ??= providerConfiguration.CurrentValue.MaxOutputTokens;

        var chatResponse = await chatClient.GetResponseAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatResponse.Messages.First())
            .Build();

        return new AnthropicResponse(prompt, chatResponse);
    }
}
