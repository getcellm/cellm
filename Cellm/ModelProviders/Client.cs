using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Cellm.ModelProviders;

internal class Client : IClient
{
    private readonly IClientFactory _clientFactory;
    private readonly CellmConfiguration _cellmConfiguration;

    internal Client(IClientFactory clientFactory, IOptions<CellmConfiguration> cellmConfiguration)
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
            throw new CellmException("API request failed", ex);
        }
        catch (JsonException ex)
        {
            throw new CellmException("Failed to deserialize API response", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new CellmException("Serialization or deserialization of request failed", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new CellmException("Null reference encountered while processing the response", ex);
        }
        
    }
}
