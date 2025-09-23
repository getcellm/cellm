using Cellm.AddIn.Exceptions;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;
using Polly.RateLimiting;
using Polly.Registry;
using Polly.Timeout;

namespace Cellm.Models;

internal class Client(ISender sender, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    public async Task<Prompt> GetResponseAsync(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        var resiliencePipeline = resiliencePipelineProvider.GetPipeline<Prompt>("RateLimiter");

        try
        {
            return await resiliencePipeline.ExecuteAsync(async innerCancellationToken =>
            {
                var response = await sender.Send(new ProviderRequest(prompt, provider), innerCancellationToken).ConfigureAwait(false);
                return response.Prompt;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (RateLimiterRejectedException ex)
        {
            throw new CellmException($"{provider}/{prompt.Options.ModelId} rate limit exceeded", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new CellmException($"{provider}/{prompt.Options.ModelId} request timed out", ex);
        }
    }
}
