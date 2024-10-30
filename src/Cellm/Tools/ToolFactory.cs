using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using System.Reflection;

namespace Cellm.Tools;

public class ToolFactory
{
    public static Tool CreateTool(Type type)
    {
        var description = GetDescriptionForType(type);

        var parameterSchemaBuilder = new JsonSchemaBuilder()
            .FromType(type)
            .Properties(GetPropertiesForType(type))
            .Required(GetRequiredForType(type))
            .AdditionalProperties(false);

        var parameterSchema = parameterSchemaBuilder.Build();
        var parameters = parameterSchema.ToJsonDocument();

        return new Tool(
            type.Name,
            description,
            parameters
        );
    }
    private static IReadOnlyDictionary<string, JsonSchema> GetPropertiesForType(Type type)
    {
        return type
            .GetProperties()
            .ToDictionary(
                property => property.Name,
                property => new JsonSchemaBuilder()
                    .FromType(property.PropertyType)
                    .Description(GetPropertyDescriptionsForType(type, property.Name))
                    .Build()
            );
    }

    private static string GetPropertyDescriptionsForType(Type type, string propertyName)
    {
        return type
            .GetConstructors()
            .First() // Records have a single constructor
            .GetParameters()
            .Where(property => property.Name == propertyName)
            .Select(property => property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description)
            .FirstOrDefault() ?? throw new CellmException($"Cannot get description of {type.Name} property {propertyName}.");
    }

    public static List<string> GetRequiredForType(Type type)
    {
        return type
            .GetConstructors()
            .First() // Records have a single constructor
            .GetParameters()
            .Where(p => !IsNullableType(p.ParameterType))
            .Select(p => p.Name!)
            .ToList();
    }

    private static bool IsNullableType(Type type)
    {
        // Check if it's a nullable reference type (marked with ?)
        if (type.IsValueType == false)
        {
            var nullabilityInfo = type.GetCustomAttribute<NullableAttribute>();
            if (nullabilityInfo != null)
            {
                return true;
            }
        }

        // Check if it's Nullable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        return false;
    }

    private static string GetDescriptionForType(Type type)
    {
        var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description ?? type.Name;
    }
}