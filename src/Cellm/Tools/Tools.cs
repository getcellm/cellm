using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Prompts;
using Json.More;
using Json.Patch;
using Json.Pointer;
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

    public async Task<string> Run(ToolCall toolCall)
    {
        var globRequest = _serde.Deserialize<GlobRequest>(toolCall.Arguments);

        return toolCall.Name switch
        {
            "Glob" => await RunGlob(globRequest),
            _ => throw new ArgumentException($"Unsupported tool: {toolCall.Name}")
        };
    }

    private async Task<string> RunGlob(GlobRequest request)
    {
        var response = await _sender.Send(request);
        return _serde.Serialize(response);
    }

    public List<Tool> GetTools()
    {
        // https://til.cazzulino.com/dotnet/how-to-emit-descriptions-for-exported-json-schema-using-jsonschemaexporter
        var builder = new JsonSchemaBuilder()
            .FromType<GlobRequest>()
            .Required("RootPath", "IncludePatterns");

        var schema = builder.Build();
        var jsonDocument = schema.ToJsonDocument();

        var patchOperations = typeof(GlobRequest)
          .GetProperties()
          .Select(x => PatchOperation.Add(JsonPointer.Parse($"/properties/{x.Name}/description"), x.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description))
          .ToList();

        var patchDescriptions = new JsonPatch(patchOperations);
        var jsonDocumentWithPatchedDescriptions = patchDescriptions.Apply(jsonDocument) ?? throw new CellmException();

        var classDescription = typeof(Glob).GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? throw new CellmException();

        return new List<Tool>()
        {
            new Tool(
                nameof(Glob),
                classDescription,
                jsonDocumentWithPatchedDescriptions
            )
        };
    }
}
