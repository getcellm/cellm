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

            if (!Enum.TryParse<Provider>(defaultProviderName, out var _))
            {
                SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
            }
        }
        catch (Exception)
        {
            // Default to Ollama if missing
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", nameof(Provider.Ollama));
        }
    }

    private void EnsureDefaultCache()
    {
        try
        {
            GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}");
        }
        catch (Exception)
        {
            // Default to false if missing
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}", "False");
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
                {ModelGroup()}
                {OutputGroup()}
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
