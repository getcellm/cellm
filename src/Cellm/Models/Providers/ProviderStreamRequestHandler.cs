using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers;

internal class ProviderStreamRequestHandler(IChatClientFactory chatClientFactory)
    : IStreamRequestHandler<ProviderStreamRequest, ChatResponseUpdate>
{
    public async IAsyncEnumerable<ChatResponseUpdate> Handle(
        ProviderStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatClient = chatClientFactory.GetClient(request.Provider);

        var stream = chatClient.GetStreamingResponseAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }
}