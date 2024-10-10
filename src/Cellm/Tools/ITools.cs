using Cellm.Prompts;

namespace Cellm.Tools;

internal interface ITools
{
    public List<Tool> GetTools();

    public Task<string> Run(ToolRequest toolRequest);
}
