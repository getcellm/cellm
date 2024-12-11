using Cellm.Models.Exceptions;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using MediatR;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Cellm.Models;

public class Client(ISender sender, IOptions<ProviderConfiguration> providerConfiguration)
{
    private readonly ProviderConfiguration _providerConfiguration = providerConfiguration.Value;

    public async Task<Prompt> Send(Prompt prompt, string? provider, Uri? baseAddress, CancellationToken cancellationToken)
    {
        try
        {
            provider ??= _providerConfiguration.DefaultProvider;

            if (!Enum.TryParse<Provider>(provider, true, out var parsedProvider))
            {
                throw new ArgumentException($"Unsupported provider: {provider}");
            }

            IModelResponse response = parsedProvider switch
            {
                Provider.Anthropic => await sender.Send(new AnthropicRequest(prompt, provider, baseAddress), cancellationToken),
                Provider.Llamafile => await sender.Send(new LlamafileRequest(prompt), cancellationToken),
                Provider.Ollama => await sender.Send(new OllamaRequest(prompt), cancellationToken),
                Provider.OpenAi => await sender.Send(new OpenAiRequest(prompt), cancellationToken),
                Provider.OpenAiCompatible => await sender.Send(new OpenAiCompatibleRequest(prompt, baseAddress), cancellationToken),
                _ => throw new InvalidOperationException($"Provider {parsedProvider} is defined but not implemented")
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
