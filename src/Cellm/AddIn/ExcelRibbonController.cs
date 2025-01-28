using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Services;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn.RibbonController;

[ComVisible(true)]
public class ExcelRibbonController : ExcelRibbon
{
    private IRibbonUI? _ribbonUi;

    private bool _baseAddressEnabled = false;
    private bool _apiKeyEnabled = false;
    private string _providerAndModel = string.Empty;

    public override string GetCustomUI(string RibbonID)
    {
        return $"""
<customUI xmlns="http://schemas.microsoft.com/office/2006/01/customui" onLoad="OnLoad">
    <ribbon>
        <tabs>
            <tab id="cellm" label="Cellm">
                {ModelGroup()}
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

    public string ModelGroup()
    {
        var modelIds = new List<string>();

        var anthropicConfiguration = ServiceLocator.Get<IOptionsMonitor<AnthropicConfiguration>>().CurrentValue;
        var deepSeekConfiguration = ServiceLocator.Get<IOptionsMonitor<DeepSeekConfiguration>>().CurrentValue;
        var googleConfiguration = ServiceLocator.Get<IOptionsMonitor<GoogleConfiguration>>().CurrentValue;
        var llamafileConfiguration = ServiceLocator.Get<IOptionsMonitor<LlamafileConfiguration>>().CurrentValue;
        var mistralConfiguration = ServiceLocator.Get<IOptionsMonitor<MistralConfiguration>>().CurrentValue;
        var ollamaConfiguration = ServiceLocator.Get<IOptionsMonitor<OllamaConfiguration>>().CurrentValue;
        var openAiConfiguration = ServiceLocator.Get<IOptionsMonitor<OpenAiConfiguration>>().CurrentValue;
        var openAiCompatibleConfiguration = ServiceLocator.Get<IOptionsMonitor<OpenAiCompatibleConfiguration>>().CurrentValue;

        modelIds.AddRange(anthropicConfiguration.Models.Select(m => $"{nameof(Provider.Anthropic)}/{m}"));
        modelIds.AddRange(deepSeekConfiguration.Models.Select(m => $"{nameof(Provider.DeepSeek)}/{m}"));
        modelIds.AddRange(googleConfiguration.Models.Select(m => $"{nameof(Provider.Google)}/{m}"));
        modelIds.AddRange(llamafileConfiguration.Models.Select(m => $"{nameof(Provider.Llamafile)}/{m}"));
        modelIds.AddRange(mistralConfiguration.Models.Select(m => $"{nameof(Provider.Mistral)}/{m}"));
        modelIds.AddRange(ollamaConfiguration.Models.Select(m => $"{nameof(Provider.Ollama)}/{m}"));
        modelIds.AddRange(openAiConfiguration.Models.Select(m => $"{nameof(Provider.OpenAi)}/{m}"));
        modelIds.AddRange(openAiCompatibleConfiguration.Models.Select(m => $"{nameof(Provider.OpenAiCompatible)}/{m}"));

        var stringBuilder = new StringBuilder();

        foreach (var modelId in modelIds)
        {
            stringBuilder.AppendLine($"<item label=\"{modelId.ToLower()}\" id=\"{new String(modelId.Where(Char.IsLetterOrDigit).ToArray())}\" />");
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
    <editBox id="baseAddress" label="Address" sizeString="WWWWWWWWWWWWWWW" enabled="{_baseAddressEnabled.ToString().ToLower()}" />
    <editBox id="apiKey" label="API Key" sizeString="WWWWWWWWWWWWWWW" enabled="{_apiKeyEnabled.ToString().ToLower()}" />
</group>
""";
    }

    public string OnGetSelectedModel(IRibbonControl control)
    {
        if (!string.IsNullOrEmpty(_providerAndModel))
        {
            return _providerAndModel;
        }

        var providerConfiguration = ServiceLocator.Get<IOptionsMonitor<ProviderConfiguration>>().CurrentValue;
        var provider = providerConfiguration.DefaultProvider;

        var configuration = ServiceLocator.Get<IConfiguration>();
        var model = configuration
            .GetSection($"{provider}Configuration")
            .GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return $"{provider.ToLower()}/{model}";
    }

    public void OnModelChanged(IRibbonControl control, string providerAndModel)
    {
        if (!Enum.TryParse<Provider>(GetProvider(providerAndModel), true, out var provider))
        {
            throw new ArgumentException($"Unsupported provider: {providerAndModel}");
        }

        var model = GetModel(providerAndModel);

        SetDefaultModelId(provider, model);

        _providerAndModel = providerAndModel;

        _ribbonUi?.Invalidate();
    }

    private void SetDefaultModelId(Provider provider, string model)
    {
        var configurationPath = ServiceLocator.ConfigurationPath;
        var localSettingsPath = Path.Combine(configurationPath, "appsettings.Local.json");

        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(localSettingsPath)!);

            // Load or create JSON root
            var rootNode = File.Exists(localSettingsPath)
                ? JsonNode.Parse(File.ReadAllText(localSettingsPath))
                : new JsonObject();

            // Create new JSON object if parsing failed
            rootNode ??= new JsonObject();

            // Update configuration sections using modern indexer syntax
            UpdateSection(rootNode, "ProviderConfiguration", "DefaultProvider", provider.ToString());
            UpdateSection(rootNode, $"{provider}Configuration", "DefaultModel", model);

            // Write with sorted properties and indentation
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Optional: Match your JSON style
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserve special characters
            };

            File.WriteAllText(localSettingsPath, rootNode.ToJsonString(options));
        }
        catch (JsonException ex)
        {
            // Handle invalid JSON format
            throw new InvalidOperationException($"Invalid JSON format in {localSettingsPath}", ex);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException or
            IOException or
            DirectoryNotFoundException
        )
        {
            // Handle file system errors
            throw new InvalidOperationException(
                $"Failed to update configuration file: {ex.Message}", ex);
        }
    }

    private static void UpdateSection(
        JsonNode rootNode,
        string sectionName,
        string propertyName,
        string value)
    {
        var section = rootNode[sectionName]?.AsObject() ?? new JsonObject();
        section[propertyName] = value;
        rootNode[sectionName] = section;
    }

    private static string GetProvider(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[..index];
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
}