using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenAiCompatible;

public interface IChatClientFactory
{
    IChatClient GetClient(Provider provider);
}