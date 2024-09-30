using Microsoft.OpenApi.Models;
using System.ComponentModel;
using System.Reflection;

internal static class OpenAiToolSchemaGenerator
{
    public static OpenApiSchema GenerateSchema<T>()
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var property in typeof(T).GetProperties())
        {
            var propertySchema = new OpenApiSchema
            {
                Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description
            };

            SetPropertyType(propertySchema, property.PropertyType);

            schema.Properties[property.Name] = propertySchema;

            if (!propertySchema.Nullable && Nullable.GetUnderlyingType(property.PropertyType) == null)
            {
                schema.Required.Add(property.Name);
            }
        }

        // Set additionalProperties to false
        schema.AdditionalPropertiesAllowed = false;

        return schema;
    }

    private static void SetPropertyType(OpenApiSchema schema, Type propertyType)
    {
        schema.Type = propertyType switch
        {
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) || t == typeof(long) => "integer",
            Type t when t == typeof(float) || t == typeof(double) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t == typeof(DateTime) => "string",
            Type t when typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string) => "array",
            _ => "object"
        };

        if (schema.Type == "string" && propertyType == typeof(DateTime))
        {
            schema.Format = "date-time";
        }

        if (schema.Type == "array")
        {
            schema.Items = new OpenApiSchema { Type = "string" };
        }

        schema.Nullable = Nullable.GetUnderlyingType(propertyType) != null;
    }
}