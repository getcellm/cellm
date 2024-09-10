using Cellm.Models.Anthropic;
using Cellm.Models.Google;
using Cellm.Models.OpenAi;
using Cellm.Services;

namespace Cellm.Models;

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
