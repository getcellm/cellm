using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.Models.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private Provider GetCurrentProvider()
    {
        return Enum.Parse<Provider>(GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"), true);
    }

    private static T GetProviderConfiguration<T>()
    {
        return CellmAddIn.Services.GetRequiredService<IOptionsMonitor<T>>().CurrentValue;
    }

    private static IProviderConfiguration GetProviderConfiguration(Provider provider)
    {
        return CellmAddIn.Services.GetRequiredService<IEnumerable<IOptionsMonitor<IProviderConfiguration>>>()
            .Select(providerConfiguration => providerConfiguration.CurrentValue)
            .Single(providerConfiguration => providerConfiguration.Id == provider);
    }

    private static Provider GetProvider(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        if (!Enum.TryParse<Provider>(providerAndModel[..index], true, out var provider))
        {
            throw new ArgumentException($"Unsupported default provider: {providerAndModel[..index]}");
        }

        return provider;
    }

    private static string GetModel(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[(index + 1)..];
    }

    public static void SetValue(string key, string value)
    {
        var keySegments = key.Split(':');
        var localNode = File.Exists(_appsettingsLocalPath)
            ? JsonNode.Parse(File.ReadAllText(_appsettingsLocalPath)) ?? new JsonObject()
            : new JsonObject();

        SetValueInNode(localNode.AsObject(), keySegments, value);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_appsettingsLocalPath)!);
        File.WriteAllText(_appsettingsLocalPath, localNode.ToJsonString(options));
    }

    private static void SetValueInNode(JsonObject node, string[] keySegments, string value)
    {
        var current = node;
        for (var i = 0; i < keySegments.Length; i++)
        {
            var isLast = i == keySegments.Length - 1;
            var segment = keySegments[i];

            if (isLast)
            {
                current[segment] = value;
            }
            else
            {
                if (!current.TryGetPropertyValue(segment, out var nextNode))
                {
                    nextNode = new JsonObject();
                    current[segment] = nextNode;
                }
                current = nextNode!.AsObject();
            }
        }
    }

    public static string GetValue(string key)
    {
        var keySegments = key.Split(':');

        // 1. Check local settings first
        if (File.Exists(_appsettingsLocalPath))
        {
            var localNode = JsonNode.Parse(File.ReadAllText(_appsettingsLocalPath));
            var value = GetValueFromNode(localNode, keySegments);
            if (value != null) return value.ToString();
        }

        // 2. Fall back to base settings
        if (File.Exists(_appSettingsPath))
        {
            var baseNode = JsonNode.Parse(File.ReadAllText(_appSettingsPath));
            var value = GetValueFromNode(baseNode, keySegments);
            if (value != null) return value.ToString();
        }

        throw new KeyNotFoundException($"Key '{key}' not found in configuration files");
    }

    private static JsonNode? GetValueFromNode(JsonNode? node, string[] keySegments)
    {
        foreach (var segment in keySegments)
        {
            node = node is JsonObject obj
                && obj.TryGetPropertyValue(segment, out var childNode)
                ? childNode
                : null;

            if (node == null) break;
        }
        return node;
    }
}
