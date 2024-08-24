using Microsoft.Extensions.Configuration;

namespace Cellm.ModelProviders;

public interface IModelProvider
{
    HttpClient GetClient(string provider);
}

public class ModelProvider : IModelProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ModelProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public HttpClient GetClient(string provider)
    {
        var clientName = _configuration.GetSection(provider).Key;
        if (string.IsNullOrEmpty(clientName))
        {
            throw new ArgumentException($"No configuration found for provider: {provider}");
        }
        return _httpClientFactory.CreateClient(clientName);
    }
}
