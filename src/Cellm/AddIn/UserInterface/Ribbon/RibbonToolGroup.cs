using Cellm.Models.Providers;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private enum ToolsGroupControlIds
    {
        ToolsGroup,
        FunctionsSplitButton,
        FunctionsButton,
        FunctionsMenu,
        FileSearchCheckBox,
        FileReaderCheckBox
    }

    public string ToolGroup()
    {
        return $"""
        <group id="{nameof(ToolsGroupControlIds.ToolsGroup)}" label="Tools">
            <splitButton id="{nameof(ToolsGroupControlIds.FunctionsSplitButton)}" size="large">
                <button id="{nameof(ToolsGroupControlIds.FunctionsButton)}" label="Functions" imageMso="FunctionWizard" screentip="Enable/disable built-in functions" />
                <menu id="{nameof(ToolsGroupControlIds.FunctionsMenu)}">
                    <checkBox id="{nameof(ToolsGroupControlIds.FileSearchCheckBox)}" label="File Search"
                         screentip="Lets a model specify glob patterns and get back matching file paths."
                         onAction="{nameof(OnFileSearchToggled)}"
                         getPressed="{nameof(OnGetFileSearchPressed)}" />
                    <checkBox id="{nameof(ToolsGroupControlIds.FileReaderCheckBox)}" label="File Reader"
                         screentip="Lets a model specify a file path and get back its content as plain text. Supports PDF, Markdown, and common text formats."
                         onAction="{nameof(OnFileReaderToggled)}"
                         getPressed="{nameof(OnGetFileReaderPressed)}" />
                 </menu>
            </splitButton>
        </group>
        """;
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
}
