using Cellm.Models.Exceptions;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using MediatR;
using Polly.Timeout;

namespace Cellm.Models;

internal class Client(ISender sender)
{
    public async Task<Prompt> Send(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        try
        {
            IModelResponse response = provider switch
            {
                Provider.Anthropic => await sender.Send(new AnthropicRequest(prompt), cancellationToken),
                Provider.DeepSeek => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.DeepSeek), cancellationToken),
                Provider.Llamafile => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.Llamafile), cancellationToken),
                Provider.Mistral => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.Mistral), cancellationToken),
                Provider.Ollama => await sender.Send(new OllamaRequest(prompt), cancellationToken),
                Provider.OpenAi => await sender.Send(new OpenAiRequest(prompt), cancellationToken),
                Provider.OpenAiCompatible => await sender.Send(new OpenAiCompatibleRequest(prompt, Provider.OpenAiCompatible), cancellationToken),
                _ => throw new NotSupportedException($"Provider {provider} is not supported")
            };

            return response.Prompt;
        }
        catch (HttpRequestException ex)
        {
            throw new CellmModelException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new CellmModelException($"Null reference exception: {ex.Message}", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new CellmModelException($"Request timed out: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not CellmModelException)
        {
            // Handle any other unexpected exceptions
            throw new CellmModelException($"An unexpected error occurred: {ex.Message}", ex);
        }
    }
}
