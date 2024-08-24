using Cellm.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.ModelProviders;

public interface IClientFactory
{
    IClient GetClient(string clientName);
}


public class ClientFactory : IClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ClientFactory(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public IClient GetClient(string modelProvider)
    { 

        return modelProvider switch
        {
            "Anthropic" => ServiceLocator.ServiceProvider.GetRequiredService<AnthropicClient>(),
            // "OpenAI" => new OpenAIClient(httpClient),
            // Add more cases as needed
            _ => throw new ArgumentException($"Unsupported client type: {modelProvider}")
        };
    }
}