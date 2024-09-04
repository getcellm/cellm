using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Polly.Timeout;

namespace Cellm.ModelProviders;

internal class Client : IClient
{
    private readonly IClientFactory _clientFactory;
    private readonly CellmAddInConfiguration _cellmConfiguration;

    public Client(IClientFactory clientFactory, IOptions<CellmAddInConfiguration> cellmConfiguration)
    {
        _clientFactory = clientFactory;
        _cellmConfiguration = cellmConfiguration.Value;
    }

    public string Send(Prompt prompt)
    {
        try
        {
            var client = _clientFactory.GetClient(_cellmConfiguration.DefaultModelProvider);
            return client.Send(prompt);
        }
        catch (HttpRequestException ex)
        {
            throw new CellmException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new CellmException($"JSON processing error: {ex.Message}", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new CellmException("Serialization or deserialization of request failed", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new CellmException("Null reference encountered while processing the response", ex);
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
