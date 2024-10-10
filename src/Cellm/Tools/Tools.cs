using System.Reflection;
using System.Text.Json;
using Cellm.Models;
using Cellm.Prompts;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using MediatR;

namespace Cellm.Tools;

internal class Tools : ITools
{
    private readonly ISender _sender;
    private readonly ISerde _serde;

    public Tools(ISender sender, ISerde serde)
    {
        _sender = sender;
        _serde = serde;
    }

    public async Task<string> Run(ToolRequest toolRequest)
    {
        return toolRequest.Name switch
        {
            "Glob" => await RunGlob(toolRequest.Arguments),
            _ => throw new ArgumentException($"Unknown tool: {toolRequest.Name}")
        };
    }

    public List<Tool> GetTools()
    {
        var schema = new JsonSchemaBuilder()
            .FromType<GlobRequest>().Build();

        var jsonDocument = schema.ToJsonDocument();

        var json = JsonSerializer.Serialize(jsonDocument);

        return new List<Tool>()
        {
            new Tool(
                "Glob",
                "Search for files on the user's disk using glob patterns. Useful when user asks you to find files.",
                new Dictionary<string, (string Description, string Type)>
                {
                    { "RootPath", ("The root directory to start the glob search from", "string") },
                    { "IncludePatterns", ("List of patterns to include in the search", "string") },
                    { "ExcludePatterns", ("Optional list of patterns to exclude from the search", "string") }
                },
                new List<string> { "RootPath", "IncludePatterns" }
            )
        };
    }

    private async Task<string> RunGlob(Dictionary<string, string> toolCall)
    {
        var globRequest = new GlobRequest(
            _serde.Deserialize<string>(toolCall[nameof(GlobRequest.RootPath)]),
            _serde.Deserialize<List<string>>(toolCall[nameof(GlobRequest.IncludePatterns)]),
            _serde.Deserialize<List<string>>(toolCall[nameof(GlobRequest.ExcludePatterns)]));

        var globResponse = await _sender.Send(globRequest);

        return _serde.Serialize(globResponse);
    }

    private T ToRequest<T>(IDictionary<string, string> dict)
    where T : class
    {
        Type type = typeof(T);
        T result = Activator.CreateInstance(type) as T ?? throw new NullReferenceException(nameof(Activator.CreateInstance));
        foreach (var item in dict)
        {
            var property = type.GetProperty(item.Key) ?? throw new NullReferenceException(item.Key);
            property.SetValue(result, item.Value, null);
        }
        return result;
    }

    private IDictionary<string, string> FromRequest<T>(T item)
        where T : class
    {
        Type myObjectType = item.GetType();
        IDictionary<string, string> dict = new Dictionary<string, string>();
        var indexer = Array.Empty<object>();
        PropertyInfo[] properties = myObjectType.GetProperties();
        foreach (var info in properties)
        {
            var value = info.GetValue(item, indexer) ?? throw new NullReferenceException(info.Name);
            dict.Add(info.Name, _serde.Serialize(value) ?? throw new NullReferenceException(nameof(value)));
        }
        return dict;
    }
}
