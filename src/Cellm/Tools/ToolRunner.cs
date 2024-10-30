using Cellm.Models;
using Cellm.Prompts;
using Cellm.Tools.Glob;
using MediatR;

namespace Cellm.Tools;

internal class ToolRunner
{
    private readonly ISender _sender;
    private readonly ISerde _serde;
    private readonly ToolFactory _toolFactory;
    private readonly IEnumerable<Type> _toolTypes;

    public ToolRunner(ISender sender, ISerde serde, ToolFactory toolFactory)
    {
        _sender = sender;
        _serde = serde;
        _toolFactory = toolFactory;
        _toolTypes = new List<Type>() { typeof(GlobRequest) };
    }

    public List<Tool> GetTools()
    {
        return _toolTypes.Select(ToolFactory.CreateTool).ToList();
    }

    public async Task<string> Run(ToolCall toolCall)
    {
        return toolCall.Name switch
        {
            nameof(GlobRequest) => await Run<GlobRequest>(toolCall.Arguments),
            _ => throw new ArgumentException($"Unsupported tool: {toolCall.Name}")
        };
    }

    private async Task<string> Run<T>(string arguments)
        where T : notnull
    {
        var request = _serde.Deserialize<T>(arguments);
        var response = await _sender.Send(request);
        return _serde.Serialize(response);
    }
}
