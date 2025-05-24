using System.Security;
using System.Text;
using Cellm.Models.Providers;
using Cellm.Tools.ModelContextProtocol;
using Cellm.Users;
using ExcelDna.Integration.CustomUI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        FileReaderCheckBox,

        McpGroup,
        McpSplitButton,
        McpButton,
        McpMenu
    }

    private const string McpCheckBoxIdPrefix = "McpCheckBox_"; // Prefix for dynamic IDs

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
            <splitButton id="{nameof(ToolsGroupControlIds.McpSplitButton)}" size="large" getEnabled="{nameof(GetMcpEnabled)}">
                <button id="{nameof(ToolsGroupControlIds.McpButton)}" label="MCP" getImage="{nameof(GetMcpMenuImage)}" screentip="Enable/disable Model Context Protocol servers." />
                <menu id="{nameof(ToolsGroupControlIds.McpMenu)}">
                    {GetMcpMenuContent()}
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

        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}", enabled.ToString());
    }

    public bool OnGetCachePressed(IRibbonControl control)
    {
        return bool.Parse(GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableCache)}"));
    }

    public string GetMcpMenuContent()
    {
        try
        {
            var modelContenxtProtocolConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;

            var numberOfStdioServers = modelContenxtProtocolConfiguration.StdioServers?.Count ?? 0;
            var numberOfSseServers = modelContenxtProtocolConfiguration.SseServers?.Count ?? 0;

            if (numberOfStdioServers + numberOfSseServers == 0)
            {
                return @"<checkBox label=""(No MCP servers configured)"" enabled=""false"" />";
            }

            var menuXml = new StringBuilder();
            foreach (var server in modelContenxtProtocolConfiguration.StdioServers ?? [])
            {
                // Skip servers without a valid name, as it's used for ID and config key
                if (string.IsNullOrWhiteSpace(server.Name)) continue;

                var checkBoxId = $"{McpCheckBoxIdPrefix}{server.Name}";
                var label = server.Name;
                var screentip = $"Enable/disable the {server.Name} MCP server ({server.Command}).";

                menuXml.AppendLine($"""
                     <checkBox id="{EncodeXmlAttribute(checkBoxId)}" label="{EncodeXmlAttribute(label)}"
                               screentip="{EncodeXmlAttribute(screentip)}"
                               onAction="{nameof(OnMcpServerToggled)}"
                               getPressed="{nameof(OnGetMcpServerPressed)}" />
                     """);
            }

            foreach (var server in modelContenxtProtocolConfiguration.SseServers ?? [])
            {
                // Skip servers without a valid name, as it's used for ID and config key
                if (string.IsNullOrWhiteSpace(server.Name)) continue;

                var checkBoxId = $"{McpCheckBoxIdPrefix}{server.Name}";
                var label = server.Name;
                var screentip = $"Enable/disable the {server.Name} MCP server ({server.Endpoint}).";

                menuXml.AppendLine($"""
                     <checkBox id="{EncodeXmlAttribute(checkBoxId)}" label="{EncodeXmlAttribute(label)}"
                               screentip="{EncodeXmlAttribute(screentip)}"
                               onAction="{nameof(OnMcpServerToggled)}"
                               getPressed="{nameof(OnGetMcpServerPressed)}" />
                     """);
            }


            return menuXml.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error generating MCP menu content: {message}", ex.Message);

            return @"<checkBox label=""(Error loading MCP servers)"" enabled=""false"" />"; ;
        }
    }

    // Helper to encode strings safely for use in XML attributes
    private static string EncodeXmlAttribute(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    public void OnMcpServerToggled(IRibbonControl control, bool pressed)
    {
        // Check if the control ID starts with the expected prefix
        if (!control.Id.StartsWith(McpCheckBoxIdPrefix)) return;

        var serverName = control.Id.Substring(McpCheckBoxIdPrefix.Length);
        if (string.IsNullOrWhiteSpace(serverName)) return; // Should not happen if ID generation is correct

        var configKey = $"ProviderConfiguration:EnableModelContextProtocolServers:{serverName}";

        try
        {
            // Update the configuration value using the SetValue helper
            SetValue(configKey, pressed.ToString());

            // Invalidate the control to trigger OnGetMcpServerPressed and refresh UI
            _ribbonUi?.InvalidateControl(control.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error setting MCP server '{serverName}' state: {message}", serverName, ex.Message);
        }
    }

    public bool OnGetMcpServerPressed(IRibbonControl control)
    {
        if (!control.Id.StartsWith(McpCheckBoxIdPrefix))
        {
            return false;
        }

        var serverName = control.Id.Substring(McpCheckBoxIdPrefix.Length);
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return false;
        }

        var configKey = $"ProviderConfiguration:EnableModelContextProtocolServers:{serverName}";

        try
        {
            var value = GetValue(configKey);

            // Default to false on failure.
            return bool.TryParse(value, out var result) && result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting MCP server '{serverName}' state: {message}", serverName, ex.Message);
            return false; // Default to disabled on error
        }
    }

    public Bitmap? GetMcpMenuImage(IRibbonControl control)
    {
        return ImageLoader.LoadEmbeddedPngResized($"{ResourcesBasePath}/mcp.png", 64, 64);
    }


    public bool GetMcpEnabled(IRibbonControl control)
    {
        var account = CellmAddIn.Services.GetRequiredService<Account>();

        return account.HasEntitlement(Entitlement.EnableModelContextProtocol);
    }
}
