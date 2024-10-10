using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn.Exceptions;
using FastSerialization;

namespace Cellm.Models;

internal class Serde : ISerde
{
    private readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Serialize<TSerialize>(TSerialize value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? _defaultOptions);
    }

    public TDeserialize Deserialize<TDeserialize>(string value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<TDeserialize>(value, options ?? _defaultOptions) ?? throw new CellmException($"Failed to deserialize {value} to {typeof(TDeserialize).Name}");
    }
}
