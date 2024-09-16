using System.Text.Json;

namespace Cellm.Models;

internal interface ISerde
{
    public string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null);

    public TValue Deserialize<TValue>(string value, JsonSerializerOptions? options = null);
}
