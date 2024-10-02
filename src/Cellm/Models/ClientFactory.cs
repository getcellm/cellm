using Cellm.Models.Anthropic;
using Cellm.Models.GoogleAi;
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
            "googleai" => ServiceLocator.Get<GoogleAiClient>(),
            "llamafile" => ServiceLocator.Get<LlamafileClient>(),
            _ => throw new ArgumentException($"Unsupported client type: {modelProvider}")
        };
    }
}
