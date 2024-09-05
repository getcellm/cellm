using Cellm.Prompts;

namespace Cellm.ModelProviders;

public interface IClient
{
    public Task<string> Send(Prompt prompt);
}