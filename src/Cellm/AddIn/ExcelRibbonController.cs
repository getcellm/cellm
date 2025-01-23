using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Services;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Office.Interop.Excel;

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

    public string AccountGroup()
    {
        return $"""
<group id="account" label="Account">
    <editBox id="email" label="Email" onChange="OnEmailChanged" sizeString="WWWWWWWWWWWWWWW"/>
    <editBox id="password" label="Password" sizeString="WWWWWWWWWWWWWWW"/>
    <box id="loginbox" boxStyle="horizontal">
        <button id="login" label="Login" onAction="OnButtonPressed"/>
        <button id="register" label="Register" onAction="OnButtonPressed"/>
        <checkBox id="rememberme" label="Remember Me" />
    </box>
</group>
""";
    }

    public string ModelGroup()
    {
        var stringBuilder = new StringBuilder();

        var anthropicConfiguration = ServiceLocator.Get<IOptions<AnthropicConfiguration>>().Value;
        foreach (var model in anthropicConfiguration.Models)
        {
            stringBuilder.AppendLine($"<item label=\"{nameof(Provider.Anthropic).ToLower()}/{model}\" id=\"A{Guid.NewGuid()}\" />");
        }

        var ollamaConfiguration = ServiceLocator.Get<IOptions<OllamaConfiguration>>().Value;
        foreach (var model in ollamaConfiguration.Models)
        {
            stringBuilder.AppendLine($"<item label=\"{nameof(Provider.Ollama).ToLower()}/{model}\" id=\"A{Guid.NewGuid()}\" />");
        }


        var openAiConfiguration = ServiceLocator.Get<IOptions<OpenAiConfiguration>>().Value;
        foreach (var model in openAiConfiguration.Models)
        {
            stringBuilder.AppendLine($"<item label=\"{nameof(Provider.OpenAi).ToLower()}/{model}\" id=\"A{Guid.NewGuid()}\"/>");
        }


        return $"""
<group id="models" label="Model">
    <comboBox id="comboBox" label="Model" sizeString="WWWWWWWWWWWWWWW" onChange="OnModelChanged">
        {stringBuilder}
    </comboBox>
    <editBox id="baseAddress" label="Address" sizeString="WWWWWWWWWWWWWWW" enabled="{_baseAddressEnabled}"/>
    <editBox id="apiKey" label="API Key" sizeString="WWWWWWWWWWWWWWW" enabled="{_apiKeyEnabled}"/>
</group>
""";
    }

    public void OnEmailChanged(IRibbonControl email)
    {
        Debug.WriteLine(email);
    }

    public void OnButtonPressed(IRibbonControl control)
    {
        _ribbonUi?.Invalidate();

        MessageBox.Show("Hello from control " + control.Id);
    }

    public void OnModelChanged(IRibbonControl control, string providerAndModel)
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