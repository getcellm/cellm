using Cellm.Prompts;

namespace Cellm.Tools;

internal interface ITools
{
    public Task<string> Run(ToolRequest toolCall);
}
