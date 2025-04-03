using Cellm.Models.Prompts;
using Cellm.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler()
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = ServiceLocator.ServiceProvider.GetRequiredKeyedService<IChatClient>(request.Provider);

        var chatResponse = await chatClient.GetResponseAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatResponse.Messages.First())
            .Build();

        return new OpenAiCompatibleResponse(prompt, chatResponse);
    }
}
