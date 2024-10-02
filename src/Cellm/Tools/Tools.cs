using System.Reflection;
using System.Text.Json;
using Cellm.Models;
using Cellm.Prompts;
using MediatR;

namespace Cellm.Tools;

internal class Tools
{
    private readonly ISender _sender;
    private readonly ISerde _serde;

    public Tools(ISender sender, ISerde serde)
    {
        _sender = sender;
        _serde = serde;
    }

    public async Task<string> RunTool(ToolRequest toolRequest)
    {
        return toolRequest.Name switch
        {
            "Glob" => await RunGlob(toolRequest.Arguments),
            _ => throw new ArgumentException($"Unknown tool: {toolRequest.Name}")
        };
    }

    private async Task<string> RunGlob(Dictionary<string, string> toolCall)
    {
        var globRequest = new GlobRequest(
            toolCall["Path"],
            _serde.Deserialize<List<string>>(toolCall["IncludePatterns"]),
            _serde.Deserialize<List<string>>(toolCall["ExcludePatterns"]));

        var globResponse = await _sender.Send(globRequest);

        return _serde.Serialize(globResponse);
    }
}
