using System.Text.Json;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Cellm.Services.ModelProviders;

internal class Client : IClient
{
    private readonly IClientFactory _clientFactory;
    private readonly CellmConfiguration _cellmConfiguration;

    public Client(IClientFactory clientFactory, IOptions<CellmConfiguration> cellmConfiguration)
    {
        _clientFactory = clientFactory;
        _cellmConfiguration = cellmConfiguration.Value;
    }

    public async Task<Prompt> Send(Prompt prompt)
    {
        var transaction = SentrySdk.StartTransaction(nameof(Client), nameof(Send));

        try
        {
            var client = _clientFactory.GetClient(_cellmConfiguration.DefaultModelProvider);
            return await client.Send(prompt);
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
        finally
        {
            transaction.Finish();
        }
    }
}
