using Cellm.Models.Exceptions;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using MediatR;
using Polly.Timeout;

namespace Cellm.Models;

public class Client(ISender sender)
{
    public async Task<Prompt> Send(Prompt prompt, Provider? provider, Uri? baseAddress, CancellationToken cancellationToken)
    {
        try
        {
            IModelResponse response = provider switch
            {
                Provider.Anthropic => await sender.Send(new AnthropicRequest(prompt, provider.ToString(), baseAddress), cancellationToken),
                Provider.Llamafile => await sender.Send(new LlamafileRequest(prompt), cancellationToken),
                Provider.Ollama => await sender.Send(new OllamaRequest(prompt), cancellationToken),
                Provider.OpenAi => await sender.Send(new OpenAiRequest(prompt), cancellationToken),
                Provider.OpenAiCompatible => await sender.Send(new OpenAiCompatibleRequest(prompt, baseAddress ?? throw new NullReferenceException($"{nameof(Provider.OpenAiCompatible)} requires BaseAddress")), cancellationToken),
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
            throw new CellmModelException($"Null reference error: {ex.Message}", ex);
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
