using Cellm.Prompts;

namespace Cellm.ModelProviders
{
    public interface IClient
    {
        string Send(Prompt prompt);
    }
}