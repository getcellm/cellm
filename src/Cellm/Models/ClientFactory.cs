using Cellm.Models.Anthropic;
using Cellm.Models.Google;
using Cellm.Models.Llamafile;
using Cellm.Models.OpenAi;
using Cellm.Services;

namespace Cellm.Models;

internal class ClientFactory : IClientFactory
{
    public IClient GetClient(string modelProvider)
    {

        return modelProvider.ToLower() switch
        {
            "anthropic" => ServiceLocator.Get<AnthropicClient>(),
            "google" => ServiceLocator.Get<GoogleClient>(),
            "openai" => ServiceLocator.Get<OpenAiClient>(),
            "llamafile" => ServiceLocator.Get<LlamafileClient>(),
            _ => throw new ArgumentException($"Unsupported client type: {modelProvider}")
        };
    }
}
