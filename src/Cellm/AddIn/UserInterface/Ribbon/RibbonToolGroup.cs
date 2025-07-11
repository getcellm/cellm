using System.Security;
using System.Text;
using Cellm.Tools.FileReader;
using Cellm.Tools.FileSearch;
using Cellm.Tools.ModelContextProtocol;
using Cellm.Users;
using ExcelDna.Integration.CustomUI;
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
        BrowserCheckBox,

        McpGroup,
        McpSplitButton,
        McpButton,
        McpMenu,
        McpAddNewButton,
        McpEditOrRemoveButton,
    }

    private const string McpCheckBoxIdPrefix = "McpCheckBox_";

    public string ToolGroup()
    {
        return $"""
        <group id="{nameof(ToolsGroupControlIds.ToolsGroup)}" label="Tools">
            <splitButton id="{nameof(ToolsGroupControlIds.FunctionsSplitButton)}" size="large">
                <button id="{nameof(ToolsGroupControlIds.FunctionsButton)}" label="Functions" imageMso="FunctionWizard" screentip="Enable/disable built-in functions" />
                <menu id="{nameof(ToolsGroupControlIds.FunctionsMenu)}">
                    <checkBox id="{nameof(ToolsGroupControlIds.BrowserCheckBox)}" label="Internet Browser"
                         screentip="Let models browse the web"
                         onAction="{nameof(OnBrowserToggled)}"
                         getPressed="{nameof(GetBrowserPressed)}" />
                    <checkBox id="{nameof(ToolsGroupControlIds.FileSearchCheckBox)}" label="File Search"
                         screentip="Let models search for files on your computer"
                         onAction="{nameof(OnFileSearchToggled)}"
                         getPressed="{nameof(GetFileSearchPressed)}" />
                    <checkBox id="{nameof(ToolsGroupControlIds.FileReaderCheckBox)}" label="File Reader"
                         screentip="Let model read files on your computer. Supports PDF, Markdown, and common text formats"
                         onAction="{nameof(OnFileReaderToggled)}"
                         getPressed="{nameof(GetFileReaderPressed)}" />
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
        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableTools)}:{nameof(FileSearchRequest)}", pressed.ToString());
    }
    public bool GetFileSearchPressed(IRibbonControl control)
    {
        var value = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableTools)}:{nameof(FileSearchRequest)}");
        return bool.Parse(value);
    }

    public void OnFileReaderToggled(IRibbonControl control, bool pressed)
    {
        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableTools)}:{nameof(FileReaderRequest)}", pressed.ToString());
    }

    public bool GetFileReaderPressed(IRibbonControl control)
    {
        var value = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableTools)}:{nameof(FileReaderRequest)}");
        return bool.Parse(value);
    }

    public void OnBrowserToggled(IRibbonControl control, bool pressed)
    {
        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableModelContextProtocolServers)}:Playwright", pressed.ToString());
    }

    public bool GetBrowserPressed(IRibbonControl control)
    {
        var value = GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableModelContextProtocolServers)}:Playwright");
        return bool.Parse(value);
    }

    public string GetMcpMenuContent()
    {
        try
        {
            var modelContenxtProtocolConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ModelContextProtocolConfiguration>>().CurrentValue;

            var anyServers = false;

            var menuXml = new StringBuilder();
            foreach (var server in modelContenxtProtocolConfiguration.StdioServers ?? [])
            {
                // Skip servers without a valid name, as it's used for ID and config key
                if (string.IsNullOrWhiteSpace(server.Name))
                {
                    continue;
                }

                // Skip Playwright, shown under built-in tools (functions)
                if (server.Name == "Playwright")
                {
                    continue;
                }

                var checkBoxId = $"{McpCheckBoxIdPrefix}{server.Name}";
                var label = server.Name;
                var screentip = $"Enable/disable the {server.Name} MCP server ({server.Command}).";

                menuXml.AppendLine($"""
                     <checkBox id="{EncodeXmlAttribute(checkBoxId)}" label="{EncodeXmlAttribute(label)}"
                               screentip="{EncodeXmlAttribute(screentip)}"
                               onAction="{nameof(OnMcpServerToggled)}"
                               getPressed="{nameof(OnGetMcpServerPressed)}" />
                     """);

                anyServers = true;
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

                anyServers = true;
            }

            if (anyServers)
            {
                menuXml.AppendLine(@"<menuSeparator id=""mcpMenuSeparator"" />");
            }

            menuXml.AppendLine(
                $@"<button id=""{nameof(ToolsGroupControlIds.McpAddNewButton)}""
                    label=""Add new ...""
                    onAction=""{nameof(ShowProviderSettingsForm)}"" />");

            menuXml.AppendLine(
                $@"<button id=""{nameof(ToolsGroupControlIds.McpEditOrRemoveButton)}""
                    label=""Edit or remove ...""
                    onAction=""{nameof(ShowProviderSettingsForm)}"" />");

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

        var configKey = $"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableModelContextProtocolServers)}:{serverName}";

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

        var configKey = $"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableModelContextProtocolServers)}:{serverName}";

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
