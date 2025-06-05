using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;
using Polly.Registry;

namespace Cellm.Models;

internal class Client(ISender sender, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    public async Task<Prompt> GetResponseAsync(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        var retryPipeline = resiliencePipelineProvider.GetPipeline<Prompt>("RateLimiter");

        return await retryPipeline.ExecuteAsync(async (pipelineCancellationToken) =>
        {
            var response = await sender.Send(new ProviderRequest(prompt, provider), pipelineCancellationToken).ConfigureAwait(false);
            return response.Prompt;
        }, cancellationToken).ConfigureAwait(false);
    }
}
