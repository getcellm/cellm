namespace Cellm.Models;

internal interface IClientFactory
{
    IClient GetClient(string clientName);
}
