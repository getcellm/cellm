using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers;

public interface IChatClientFactory
{
    IChatClient GetClient(Provider provider);
}