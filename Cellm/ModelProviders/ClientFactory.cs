namespace Cellm.ModelProviders;

public class ClientFactory : IClientFactory
{
    public IClient GetClient(string modelProvider)
    {

        return modelProvider switch
        {
            nameof(AnthropicClient) => ServiceLocator.Get<AnthropicClient>(),
            nameof(GoogleGeminiClient) => ServiceLocator.Get<GoogleGeminiClient>(),
            nameof(OpenAiClient) => ServiceLocator.Get<OpenAiClient>(),
            _ => throw new ArgumentException($"Unsupported client type: {modelProvider}")
        };
    }
}
