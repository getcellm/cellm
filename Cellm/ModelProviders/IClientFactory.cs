namespace Cellm.ModelProviders;

internal interface IClientFactory
{
    IClient GetClient(string clientName);
}
