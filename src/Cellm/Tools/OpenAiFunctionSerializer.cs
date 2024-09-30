using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cellm.Tools;

public static class OpenAiFunctionSerializer
{
    public static string SerializeFunction<TRequest, TResponse>(string functionName)
    {
        var function = new
        {
            type = "function",
            function = new
            {
                name = functionName,
                description = GetFunctionDescription<TRequest, TResponse>(),
                @strict = true,
                parameters = new
                {
                    type = "object",
                    properties = GetProperties(typeof(TRequest)),
                    required = GetRequiredProperties(typeof(TRequest)),
                    additionalProperties = false
                }
            }
        };

        return JsonSerializer.Serialize(new[] { function }, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static string GetFunctionDescription<TRequest, TResponse>() =>
        typeof(IFunction<TRequest, TResponse>)
            .GetMethod("Handle")
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description ?? string.Empty;

    private static Dictionary<string, object> GetProperties(Type type) =>
        type.GetProperties().ToDictionary(
            prop => prop.Name,
            prop => GetPropertyDetails(prop));

    private static object GetPropertyDetails(PropertyInfo prop)
    {
        var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        var type = GetJsonType(prop.PropertyType);
        var details = new Dictionary<string, object>
        {
            ["type"] = type,
            ["description"] = description
        };

        if (type == "array")
        {
            details["items"] = new { type = GetJsonType(prop.PropertyType.GetGenericArguments()[0]) };
        }

        return details;
    }

    private static List<string> GetRequiredProperties(Type type) =>
        type.GetProperties()
            .Where(prop => !IsNullable(prop.PropertyType))
            .Select(prop => prop.Name)
            .ToList();

    private static bool IsNullable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ||
        !type.IsValueType;

    private static string GetJsonType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "array";
        return "object";
    }
}