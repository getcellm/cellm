using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.User;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.RibbonController;

[ComVisible(true)]
public class Ribbon : ExcelRibbon
{
    private IRibbonUI? _ribbonUi;

    private static readonly string _appSettingsPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.json");
    private static readonly string _appsettingsLocalPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.Local.json");

    public Ribbon()
    {
        EnsureDefaultProvider();
        EnsureDefaultCache();
    }

    private void EnsureDefaultProvider()
    {
        try
        {
            // Verify if default provider exists
            GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}");
        }
        catch (KeyNotFoundException)
        {
            // Set default if missing
            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
        }
    }

    private void EnsureDefaultCache()
    {
        try
        {
            // Check if EnableCache exists
            GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}");
        }
        catch (KeyNotFoundException)
        {
            // Set default to false if missing
            SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}", "False");
        }
    }

    public override string GetCustomUI(string RibbonID)
    {
        return $"""
<customUI xmlns="http://schemas.microsoft.com/office/2006/01/customui" onLoad="OnLoad">
    <ribbon>
        <tabs>
            <tab id="cellm" label="Cellm">
                {ModelGroup()}
                {BehaviorGroup()}
            </tab>
        </tabs>
    </ribbon>
</customUI>
""";
    }

    public void OnLoad(IRibbonUI ribbonUi)
    {
        _ribbonUi = ribbonUi;
    }

    public string BehaviorGroup()
    {
        return $"""
<group id="tools" label="Tools">
    <splitButton id="Functions" size="large">
        <button id="functionsButton" label="Functions" imageMso="FunctionWizard" screentip="Enable/disable built-in functions" />
        <menu id="functionsMenu">
            <checkBox id="filesearch" label="File Search" 
                 screentip="Lets a model specify glob patterns and get back matching file paths."
                 onAction="OnFileSearchToggled"
                 getPressed="OnGetFileSearchPressed" />
        <checkBox id="filereader" label="File Reader" 
                 screentip="Lets a model specify a file path and get back its content as plain text. Supports PDF, Markdown, and common text formats."
                 onAction="OnFileReaderToggled"
                 getPressed="OnGetFileReaderPressed" />
         </menu>
    </splitButton>
</group>
""";
    }

    public string ModelGroup()
    {
        var providerAndModels = new List<string>();

        var accountConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AccountConfiguration>>().CurrentValue;

        if (accountConfiguration.IsEnabled)
        {
            var cellmConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmConfiguration>>().CurrentValue;
            providerAndModels.AddRange(cellmConfiguration.Models.Select(m => $"{nameof(Provider.Cellm)}/{m}"));
        }

        var anthropicConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>().CurrentValue;

        var deepSeekConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>().CurrentValue;
        var mistralConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<MistralConfiguration>>().CurrentValue;
        var ollamaConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>().CurrentValue;
        var openAiConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>().CurrentValue;
        var openAiCompatibleConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>().CurrentValue;

        providerAndModels.AddRange(anthropicConfiguration.Models.Select(m => $"{nameof(Provider.Anthropic)}/{m}"));
        providerAndModels.AddRange(deepSeekConfiguration.Models.Select(m => $"{nameof(Provider.DeepSeek)}/{m}"));
        providerAndModels.AddRange(mistralConfiguration.Models.Select(m => $"{nameof(Provider.Mistral)}/{m}"));
        providerAndModels.AddRange(ollamaConfiguration.Models.Select(m => $"{nameof(Provider.Ollama)}/{m}"));
        providerAndModels.AddRange(openAiConfiguration.Models.Select(m => $"{nameof(Provider.OpenAi)}/{m}"));
        providerAndModels.AddRange(openAiCompatibleConfiguration.Models.Select(m => $"{nameof(Provider.OpenAiCompatible)}/{m}"));

        var stringBuilder = new StringBuilder();

        foreach (var providerAndModel in providerAndModels)
        {
            stringBuilder.AppendLine($"<item label=\"{providerAndModel.ToLower()}\" id=\"{new String(providerAndModel.Where(Char.IsLetterOrDigit).ToArray())}\" />");
        }

        return $"""
<group id="models" label="Provider">
    <comboBox id="comboBox" 
        label="Model" 
        sizeString="WWWWWWWWWWWWWWW" 
        onChange="OnModelChanged"
        getText="OnGetSelectedModel">
        {stringBuilder}
    </comboBox>
    <editBox id="baseAddress" label="Address" sizeString="WWWWWWWWWWWWWWW" getEnabled="OnGetBaseAddressEnabled" getText="OnGetBaseAddress" onChange="OnBaseAddressChanged" />
    <editBox id="apiKey" label="API Key" sizeString="WWWWWWWWWWWWWWW" getEnabled="OnGetApiKeyEnabled" getText="OnGetApiKey" onChange="OnApiKeyChanged" />
    <toggleButton id="cache" label="Cache" size="large" imageMso="SourceControlRefreshStatus" 
        screentip="Enable/disable local caching of model responses. Disabling cache will clear all cached responses." 
        onAction="OnCacheToggled" getPressed="OnGetCachePressed" />
</group>
""";
    }

    public string OnGetSelectedModel(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        var model = GetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}");
        return $"{provider.ToString().ToLower()}/{model}";
    }

    public string OnGetBaseAddress(IRibbonControl control)
    {
        var provider = GetCurrentProvider();

        return provider switch
        {
            Provider.DeepSeek => GetProviderConfiguration<DeepSeekConfiguration>().BaseAddress.ToString(),
            Provider.Mistral => GetProviderConfiguration<MistralConfiguration>().BaseAddress.ToString(),
            Provider.Ollama => GetProviderConfiguration<OllamaConfiguration>().BaseAddress.ToString(),
            Provider.OpenAiCompatible => GetProviderConfiguration<OpenAiCompatibleConfiguration>().BaseAddress.ToString(),
            _ => "Built-in"
        };
    }

    public string OnGetApiKey(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return GetValue($"{provider}Configuration:ApiKey");
    }

    public void OnModelChanged(IRibbonControl control, string providerAndModel)
    {
        var provider = GetProvider(providerAndModel);
        var model = GetModel(providerAndModel);

        SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", provider.ToString());
        SetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.DefaultModel)}", model);

        _ribbonUi?.Invalidate();
    }

    public void OnBaseAddressChanged(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        SetValue($"{provider}Configuration:{nameof(DeepSeekConfiguration.BaseAddress)}", text);
    }

    public void OnApiKeyChanged(IRibbonControl control, string text)
    {
        var provider = GetCurrentProvider();
        SetValue($"{provider}Configuration:{nameof(OpenAiConfiguration.ApiKey)}", text);
    }

    public bool OnGetBaseAddressEnabled(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return provider switch
        {
            Provider.OpenAiCompatible or Provider.Ollama => true,
            _ => false
        };
    }

    public bool OnGetApiKeyEnabled(IRibbonControl control)
    {
        var provider = GetCurrentProvider();
        return provider switch
        {
            Provider.OpenAiCompatible or Provider.Anthropic or Provider.DeepSeek
                or Provider.Mistral or Provider.OpenAi => true,
            _ => false
        };
    }

    public async Task OnCacheToggled(IRibbonControl control, bool enabled)
    {
        if (!enabled)
        {
            var cache = CellmAddIn.Services.GetRequiredService<HybridCache>();
            await cache.RemoveByTagAsync(nameof(ProviderResponse));

        }

        SetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}", enabled.ToString());
    }

    public bool OnGetCachePressed(IRibbonControl control)
    {
        return bool.Parse(GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.EnableCache)}"));
    }

    public void OnFileSearchToggled(IRibbonControl control, bool pressed)
    {
        SetValue("ProviderConfiguration:EnableTools:FileSearchRequest", pressed.ToString());
    }

    public bool OnGetFileSearchPressed(IRibbonControl control)
    {
        var value = GetValue("ProviderConfiguration:EnableTools:FileSearchRequest");
        return bool.Parse(value);
    }

    public void OnFileReaderToggled(IRibbonControl control, bool pressed)
    {
        SetValue("ProviderConfiguration:EnableTools:FileReaderRequest", pressed.ToString());
    }

    public bool OnGetFileReaderPressed(IRibbonControl control)
    {
        var value = GetValue("ProviderConfiguration:EnableTools:FileReaderRequest");
        return bool.Parse(value);
    }

    private Provider GetCurrentProvider()
    {
        return Enum.Parse<Provider>(GetValue($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}"), true);
    }

    private static T GetProviderConfiguration<T>()
    {
        return CellmAddIn.Services.GetRequiredService<IOptionsMonitor<T>>().CurrentValue;
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

    public static void SetValue(string key, string value)
    {
        var keySegments = key.Split(':');
        JsonNode localNode = File.Exists(_appsettingsLocalPath)
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

    private static void SetValueInNode(JsonObject node, string[] keySegments, string value)
    {
        var current = node;
        for (int i = 0; i < keySegments.Length; i++)
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
}
