using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Aws;
using Cellm.Models.Providers.Azure;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
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
        return GetProviderConfigurations().Single(providerConfigurations => providerConfigurations.Id == provider);
    }

    private static IProviderConfiguration GetProviderConfiguration(string providerAsString)
    {
        if (Enum.TryParse<Provider>(providerAsString, out var provider))
        {
            return GetProviderConfigurations().Single(providerConfigurations => providerConfigurations.Id == provider);
        }

        throw new ArgumentException($"Invalid provider: {providerAsString}");
    }

    private static IEnumerable<IProviderConfiguration> GetProviderConfigurations()
    {
        return
        [
            // Retrieve the current, up-to-date configuration for each provider
            // Until we find a better way to inject up-to-date configuration
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AwsConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AzureConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<GeminiConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<MistralConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>().CurrentValue,
            CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>().CurrentValue
        ];
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

    public static void SetValue(string key, JsonNode value)
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

    private static void SetValueInNode(JsonObject node, string[] keySegments, JsonNode value)
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

    public static JsonNode? GetValueAsJsonNode(string key)
    {
        var keySegments = key.Split(':');

        JsonNode? localValue = null;
        JsonNode? baseValue = null;

        // 1. Check local settings
        if (File.Exists(_appsettingsLocalPath))
        {
            var localNode = JsonNode.Parse(File.ReadAllText(_appsettingsLocalPath));
            localValue = GetValueFromNode(localNode, keySegments);
        }

        // 2. Check base settings
        if (File.Exists(_appSettingsPath))
        {
            var baseNode = JsonNode.Parse(File.ReadAllText(_appSettingsPath));
            baseValue = GetValueFromNode(baseNode, keySegments);
        }

        // 3. Merge arrays if both exist and are arrays
        if (localValue is JsonArray localArray && baseValue is JsonArray baseArray)
        {
            var mergedArray = new JsonArray();

            // Add base values first
            foreach (var item in baseArray)
            {
                mergedArray.Add(item?.DeepClone());
            }

            // Add local values
            foreach (var item in localArray)
            {
                mergedArray.Add(item?.DeepClone());
            }

            return mergedArray;
        }

        // 4. Return local value if it exists, otherwise base value
        return localValue ?? baseValue;
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
