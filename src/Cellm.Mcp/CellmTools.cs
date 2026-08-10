using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Cellm.Mcp;

[McpServerToolType]
internal class CellmTools(CellmClient client)
{
    [McpServerTool(
        Name = "cellm_prompt",
        Title = "Run a Cellm prompt in Excel",
        Destructive = true,
        Idempotent = false,
        OpenWorld = true,
        UseStructuredContent = true)]
    [Description("Writes a PROMPT or PROMPTMODEL formula to an Excel cell and waits for Excel to return the result.")]
    public Task<PromptResponse> PromptAsync(
        [Description("The name of the open Excel workbook.")] string workbook,
        [Description("The name of the worksheet in that workbook.")] string worksheet,
        [Description("The target cell address, for example B2.")] string cell,
        [Description("The prompt text sent to the configured model.")] string prompt,
        [Description("Optional cell or range addresses on the same worksheet to include as context.")] string[]? ranges = null,
        [Description("Optional provider and model in provider/model form. Omit this to use Cellm's configured default.")] string? providerAndModel = null,
        [Description("Whether Cellm may replace an existing value or formula in the target cell.")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        return client.PromptAsync(
            new PromptRequest(
                Version: 1,
                Method: "prompt",
                Workbook: workbook,
                Worksheet: worksheet,
                Cell: cell,
                Prompt: prompt,
                Ranges: ranges ?? [],
                ProviderAndModel: providerAndModel,
                Overwrite: overwrite),
            cancellationToken);
    }
}
