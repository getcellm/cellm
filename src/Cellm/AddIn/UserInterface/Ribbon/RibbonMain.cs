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
        InitializeSelectedProviderIndex();
        EnsureDefaultProvider();
        EnsureDefaultCache();

        var loggerFactory = CellmAddIn.Services.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<RibbonMain>();
    }

    private int EnsureDefaultProvider()
    {
        try
        {
            var providerName = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}");
            var provider = Enum.Parse<Provider>(providerName, true);
            var item = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(provider.ToString(), StringComparison.OrdinalIgnoreCase));
            if (item.Value != null)
            {
                return item.Key;
            }

            // If provider exists in config but not in our _providerItems, fallback
            throw new KeyNotFoundException("Provider found in config but not in UI list.");
        }
        catch (KeyNotFoundException)
        {
            // Set default if missing 
            var defaultProviderName = nameof(Provider.Ollama);
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", defaultProviderName);
            var item = _providerItems.FirstOrDefault(kvp => kvp.Value.Label.Equals(defaultProviderName, StringComparison.OrdinalIgnoreCase));
            return item.Value != null ? item.Key : 3; // Use 3 (Ollama's index) as a hard fallback
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in EnsureDefaultProvider: {message}. Falling back to index 0.", ex.Message);

            // General fallback if parsing or other issues occur
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}", _providerItems[3].Label);
            return 0;
        }
    }

    private void EnsureDefaultCache()
    {
        try
        {
            // Check if EnableCache exists
            GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}");
        }
        catch (KeyNotFoundException)
        {
            // Set default to false if missing
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
