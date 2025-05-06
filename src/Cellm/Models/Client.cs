using System.Runtime.CompilerServices;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.AI;
using Polly.Registry;

namespace Cellm.Models;

internal class Client(ISender sender, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    public async Task<Prompt> GetResponseAsync(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        var retryPipeline = resiliencePipelineProvider.GetPipeline("RateLimiter");

        return await retryPipeline.ExecuteAsync(async (innerCancellationToken) =>
        {
            var response = await sender.Send(new ProviderRequest(prompt, provider), innerCancellationToken);
            return response.Prompt;
        }, cancellationToken);
    }

    internal async IAsyncEnumerable<ChatResponseUpdate> GetStreamResponseAsync(
        Prompt prompt,
        Provider provider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var retryPipeline = resiliencePipelineProvider.GetPipeline("RateLimiter");

        var stream = retryPipeline.Execute((context) => // Polly v8 often takes a CancellationToken
        {
            var stream = sender.CreateStream(
                new ProviderStreamRequest(prompt, provider),
                cancellationToken);

            // This is the stream itself, not the result of the stream, so the retry pipeline
            // will retry on the stream creation, not on the stream consumption.
            return stream;  
                              
        }, cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }
}
