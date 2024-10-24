﻿using System.Text.Json;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Anthropic;
using Cellm.Models.GoogleAi;
using Cellm.Models.Llamafile;
using Cellm.Models.OpenAi;
using Cellm.Prompts;
using MediatR;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Cellm.Models;

internal class Client : IClient
{
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly ISender _sender;

    public Client(IOptions<CellmConfiguration> cellmConfiguration, ISender sender)
    {
        _cellmConfiguration = cellmConfiguration.Value;
        _sender = sender;
    }

    public async Task<Prompt> Send(Prompt prompt, string? provider, Uri? baseAddress)
    {
        try
        {
            provider ??= _cellmConfiguration.DefaultProvider;

            if (!Enum.TryParse<Providers>(provider, true, out var parsedProvider))
            {
                throw new ArgumentException($"Unsupported provider: {provider}");
            }

            IModelResponse response = parsedProvider switch
            {
                Providers.Anthropic => await _sender.Send(new AnthropicRequest(prompt, provider, baseAddress)),
                Providers.GoogleAi => await _sender.Send(new GoogleAiRequest(prompt, provider, baseAddress)),
                Providers.Llamafile => await _sender.Send(new LlamafileRequest(prompt)),
                Providers.OpenAi => await _sender.Send(new OpenAiRequest(prompt, provider, baseAddress)),
                _ => throw new InvalidOperationException($"Provider {parsedProvider} is defined but not implemented")
            };

            return response.Prompt;
        }
        catch (HttpRequestException ex)
        {
            throw new CellmException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new CellmException($"JSON processing failed: {ex.Message}", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new CellmException($"Method not supported: {ex.Message}", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new CellmException($"Null reference error: {ex.Message}", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new CellmException($"Request timed out: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not CellmException)
        {
            // Handle any other unexpected exceptions
            throw new CellmException($"An unexpected error occurred: {ex.Message}", ex);
        }
    }
}
