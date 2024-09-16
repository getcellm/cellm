using System.Text.Encodings.Web;
using System.Text.Json;
using Cellm.AddIn.Exceptions;

namespace Cellm.Models;

internal class Serde : ISerde
{
    private readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? _defaultOptions);
    }

    public TValue Deserialize<TValue>(string value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<TValue>(value, options ?? _defaultOptions) ?? throw new CellmException("Failed to deserialize responds");
    }
}
