using Cellm.Mcp;
using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests.Integration;

[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net9.0-windows\Cellm-AddIn")]
[Trait("Category", "Excel")]
[Trait("Category", "Integration")]
public class McpPromptTests : IDisposable
{
    private readonly Workbook _workbook;

    public McpPromptTests()
    {
        _workbook = Util.Application.Workbooks.Add();
    }

    public void Dispose()
    {
        try
        {
            _workbook.Close(SaveChanges: false);
        }
        catch
        {
            // Ignore cleanup errors - COM objects may already be released
        }
    }

    [ExcelFact]
    public void Prompt_UsesExcelFormulaPathAndReturnsOutcome()
    {
        var worksheet = (Worksheet)_workbook.Worksheets[1];
        var client = new CellmClient();
        var request = new PromptRequest(
            1,
            "prompt",
            _workbook.Name,
            worksheet.Name,
            "B2",
            "What is 2+2?",
            [],
            "NotAProvider/not-a-model",
            false);

        var responseTask = Task.Run(() => client.PromptAsync(request, CancellationToken.None));
        Automation.WaitFor(() => responseTask.IsCompleted, 30_000);

        Assert.True(responseTask.IsCompletedSuccessfully, responseTask.Exception?.ToString());
#pragma warning disable VSTHRD002 // ExcelDna.Testing's wait above pumps Excel until the task completes.
        var response = responseTask.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        Assert.Equal("completed", response.Status);
        Assert.Equal("=PROMPTMODEL(\"NotAProvider/not-a-model\", \"What is 2+2?\")", response.Formula);
        Assert.Equal(response.Formula, Convert.ToString(((dynamic)worksheet.Range["B2"]).Formula2));
        Assert.Contains("Unsupported provider", response.Value?.ToString());
    }
}
