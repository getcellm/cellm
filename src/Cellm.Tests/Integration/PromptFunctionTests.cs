using Cellm.Tests.Integration.Helpers;
using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests.Integration;

[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net9.0-windows\Cellm-AddIn")]
[Trait("Category", "Excel")]
public class PromptFunctionTests : IDisposable
{
    private readonly Workbook _testWorkbook;

    public PromptFunctionTests()
    {
        var app = Util.Application;
        _testWorkbook = app.Workbooks.Add();
    }

    public void Dispose()
    {
        try
        {
            _testWorkbook?.Close(SaveChanges: false);
        }
        catch
        {
            // Ignore cleanup errors - COM objects may already be released
        }
    }

    [ExcelFact]
    [Trait("Category", "Ollama")]
    public void TestPrompt_WithOllama()
    {
        // Use unique cells (D1:D2) to avoid async cache collisions with other tests
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["D1"].Value = "Respond with exactly: Hello World";
        ws.Range["D2"].Formula = "=PROMPTMODEL(\"Ollama/gemma3:4b-it-qat\", D1)";
        ExcelTestHelper.WaitForCellNotNA(ws.Range["D2"], timeoutSeconds: 120);
        var result = ws.Range["D2"].Value?.ToString() ?? string.Empty;
        Assert.Contains("Hello", result, StringComparison.OrdinalIgnoreCase);
    }

    [ExcelFact]
    [Trait("Category", "Mistral")]
    public void TestPromptModel_Mistral()
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Respond with exactly: Hello World";
        ws.Range["A2"].Formula = "=PROMPTMODEL(\"Mistral/mistral-small-latest\",A1)";
        ExcelTestHelper.WaitForCellNotNA(ws.Range["A2"], timeoutSeconds: 60);
        var result = ws.Range["A2"].Value?.ToString() ?? string.Empty;
        Assert.Contains("Hello", result, StringComparison.OrdinalIgnoreCase);
    }
}
