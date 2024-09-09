namespace Cellm.Services.ModelProviders;

internal interface IClientFactory
{
    IClient GetClient(string clientName);
}
