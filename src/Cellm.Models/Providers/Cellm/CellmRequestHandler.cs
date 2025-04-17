using Cellm.Models.Prompts;
using Cellm.User;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.Cellm;

internal class CellmRequestHandler(Account account, [FromKeyedServices(Provider.Cellm)] IChatClient chatClient)
    : IModelRequestHandler<CellmRequest, CellmResponse>
{

    public async Task<CellmResponse> Handle(CellmRequest request, CancellationToken cancellationToken)
    {
        await account.RequireEntitlementAsync(Entitlement.EnableCellmProvider);

        var chatCompletion = await chatClient.GetResponseAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessages(chatCompletion.Messages)
            .Build();

        return new CellmResponse(prompt, chatCompletion);
    }
}
