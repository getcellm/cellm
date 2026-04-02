using System.Runtime.InteropServices;
using Cellm.Models.Providers;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn.UserInterface.Ribbon;

[ComVisible(true)]
public partial class RibbonMain : ExcelRibbon
{
    internal static IRibbonUI? _ribbonUi;

    private static readonly string _appSettingsPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.json");
    private static readonly string _appsettingsLocalPath = Path.Combine(CellmAddIn.ConfigurationPath, "appsettings.Local.json");

    private readonly ILogger<RibbonMain> _logger;
    private const string ResourcesBasePath = "AddIn/UserInterface/Resources";

    public RibbonMain()
    {
        EnsureDefaultProvider();
        EnsureDefaultCache();

        var loggerFactory = CellmAddIn.Services.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<RibbonMain>();
    }

    private void EnsureDefaultProvider()
    {
        try
        {
            var defaultProviderName = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");

            if (!Enum.TryParse<Provider>(defaultProviderName, true, out var _))
            {
                SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
            }
        }
        catch (Exception ex)
        {
            // Default to Ollama if missing
            _logger.LogDebug(ex, "Default provider not configured, defaulting to Ollama");
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
        }
    }

    private void EnsureDefaultCache()
    {
        try
        {
            GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}");
        }
        catch (Exception ex)
        {
            // Default to false if missing
            _logger.LogDebug(ex, "Cache setting not configured, defaulting to disabled");
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}", false);
        }
    }

    public override string GetCustomUI(string RibbonID)
    {
        var xml = $"""
<customUI xmlns="http://schemas.microsoft.com/office/2006/01/customui" onLoad="OnLoad" loadImage="GetEmbeddedImage">
    <ribbon>
        <tabs>
            <tab id="cellm" label="Cellm">
                {UserGroup()}
                {PromptGroup()}
                {ModelGroup()}
                {ToolGroup()}
            </tab>
        </tabs>
    </ribbon>
</customUI>
""";
        return xml;
    }

    public void OnLoad(IRibbonUI ribbonUi)
    {
        _ribbonUi = ribbonUi;
    }
}
