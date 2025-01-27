using System.Runtime.InteropServices;
using System.Text;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
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

        var anthropicConfiguration = ServiceLocator.Get<IOptions<AnthropicConfiguration>>().Value;
        var deepSeekConfiguration = ServiceLocator.Get<IOptions<DeepSeekConfiguration>>().Value;
        var llamafileConfiguration = ServiceLocator.Get<IOptions<LlamafileConfiguration>>().Value;
        var mistralConfiguration = ServiceLocator.Get<IOptions<MistralConfiguration>>().Value;
        var ollamaConfiguration = ServiceLocator.Get<IOptions<OllamaConfiguration>>().Value;
        var openAiConfiguration = ServiceLocator.Get<IOptions<OpenAiConfiguration>>().Value;
        var openAiCompatibleConfiguration = ServiceLocator.Get<IOptions<OpenAiCompatibleConfiguration>>().Value;

        modelIds.AddRange(anthropicConfiguration.Models.Select(m => $"{nameof(Provider.Anthropic)}/{m}"));
        modelIds.AddRange(deepSeekConfiguration.Models.Select(m => $"{nameof(Provider.DeepSeek)}/{m}"));
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

    public void OnModelChanged(IRibbonControl control, string providerAndModel, int selectedIndex)
    {
        if (!Enum.TryParse<Provider>(GetProvider(providerAndModel), true, out var provider))
        {
            throw new ArgumentException($"Unsupported provider: {providerAndModel}");
        }

        var model = GetModel(providerAndModel);

        SetConfiguration($"{nameof(ProviderConfiguration)}:{nameof(ProviderConfiguration.DefaultProvider)}", model);
        SetConfiguration($"{provider}Configuration:{nameof(ProviderConfiguration.DefaultModel)}", model);

        _ribbonUi?.Invalidate();
    }

    public string OnGetSelectedModel(IRibbonControl control)
    {
        var providerConfiguration = ServiceLocator.Get<IOptions<ProviderConfiguration>>().Value;
        var provider = providerConfiguration.DefaultProvider;

        var configuration = ServiceLocator.Get<IConfiguration>();
        var model = configuration
            .GetSection($"{provider}Configuration")
            .GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return $"{provider.ToLower()}/{model}";
    }

    private void SetConfiguration(string key, string value)
    {
        
        // _configuration[key] = value;
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