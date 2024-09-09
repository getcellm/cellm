using Cellm.AddIn.Prompts;

namespace Cellm.Services.ModelProviders;

internal interface IClient
{
    public Task<Prompt> Send(Prompt prompt);
}