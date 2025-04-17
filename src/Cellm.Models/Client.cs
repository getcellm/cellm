using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
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
            IModelResponse response = provider switch
            {
                Provider.Anthropic => await sender.Send(new AnthropicRequest(prompt), cancellationToken),
                Provider.DeepSeek => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.DeepSeek), cancellationToken),
                Provider.Mistral => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.Mistral), cancellationToken),
                Provider.Ollama => await sender.Send(new OllamaRequest(prompt), cancellationToken),
                Provider.OpenAi => await sender.Send(new OpenAiRequest(prompt), cancellationToken),
                Provider.OpenAiCompatible => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.OpenAiCompatible), cancellationToken),
                _ => throw new NotSupportedException($"Provider {provider} is not supported")
            };

            return response.Prompt;
        });
    }
}
