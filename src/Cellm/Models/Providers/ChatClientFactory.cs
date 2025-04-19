using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;


namespace Cellm.Models.Providers;

public class ChatClientFactory(IServiceProvider serviceProvider) : IChatClientFactory
{
    public IChatClient GetClient(Provider provider)
    {
        return serviceProvider.GetRequiredKeyedService<IChatClient>(provider);
    }
}
