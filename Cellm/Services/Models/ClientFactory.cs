using Cellm.Services;
using Cellm.Services.ModelProviders.Anthropic;
using Cellm.Services.ModelProviders.Google;
using Cellm.Services.ModelProviders.OpenAi;

namespace Cellm.Services.ModelProviders;

internal class ClientFactory : IClientFactory
{
    public IClient GetClient(string modelProvider)
    {

        return modelProvider switch
        {
            nameof(AnthropicClient) => ServiceLocator.Get<AnthropicClient>(),
            nameof(GoogleClient) => ServiceLocator.Get<GoogleClient>(),
            nameof(OpenAiClient) => ServiceLocator.Get<OpenAiClient>(),
            _ => throw new ArgumentException($"Unsupported client type: {modelProvider}")
        };
    }
}
