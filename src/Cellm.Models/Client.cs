using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;
using Polly.Registry;

namespace Cellm.Models;

internal class Client(ISender sender, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    public async Task<Prompt> Send(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        var retryPipeline = resiliencePipelineProvider.GetPipeline("RateLimiter");

        return await retryPipeline.Execute(async () =>
        {
            var response = await sender.Send(new ProviderRequest(prompt, provider));
            return response.Prompt;
        });
    }
}
