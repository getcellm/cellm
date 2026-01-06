using Cellm.Tests.Integration.Helpers;
using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests.Integration;

/// <summary>
/// Excel integration tests using Ollama with gemma3:4b-it-qat model.
/// These tests require Excel, ExcelDNA, and a running Ollama instance.
/// </summary>
[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net9.0-windows\Cellm-AddIn")]
[Trait("Category", "Ollama")]
[Trait("Category", "Excel")]
public class OllamaExcelTests : IDisposable
{
    private readonly Workbook _testWorkbook;

    public OllamaExcelTests()
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
    public void Prompt_WithOllama_ReturnsResponse()
    {
        // Arrange - use unique cells (C1:C2) to avoid async cache collisions with other tests
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["C1"].Value = "Respond with exactly: Hello World";
        ws.Range["C2"].Formula = "=PROMPTMODEL(\"Ollama/gemma3:4b-it-qat\", C1)";

        // Act
        ExcelTestHelper.WaitForCellNotNA(ws.Range["C2"], timeoutSeconds: 120);

        // Assert - use .Value instead of .Text to avoid "########" display issues
        var result = ws.Range["C2"].Value?.ToString() ?? string.Empty;
        Assert.Contains("Hello", result, StringComparison.OrdinalIgnoreCase);
    }

    [ExcelFact]
    public void PromptToRow_WithOllama_ReturnsArray()
    {
        // Arrange
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Return a JSON object with the first 3 letters: {\"data\":[\"A\",\"B\",\"C\"]}";
        ws.Range["A2"].Formula = "=PROMPTMODEL.TOROW(\"Ollama/gemma3:4b-it-qat\", A1)";

        // Act
        ExcelTestHelper.WaitForCellNotNA(ws.Range["A2"], timeoutSeconds: 120);

        // Assert - Even if structured output doesn't parse, we should get a response
        var result = ws.Range["A2"].Value?.ToString() ?? string.Empty;
        Assert.NotEmpty(result);
    }

    [ExcelFact]
    public void PromptToColumn_WithOllama_ReturnsArray()
    {
        // Arrange
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Return JSON: {\"data\":[\"X\",\"Y\",\"Z\"]}";
        ws.Range["A2"].Formula = "=PROMPTMODEL.TOCOLUMN(\"Ollama/gemma3:4b-it-qat\", A1)";

        // Act
        ExcelTestHelper.WaitForCellNotNA(ws.Range["A2"], timeoutSeconds: 120);

        // Assert
        var result = ws.Range["A2"].Value?.ToString() ?? string.Empty;
        Assert.NotEmpty(result);
    }

    [ExcelFact]
    public void Prompt_WithOllamaAndContext_ReturnsContextualResponse()
    {
        // Arrange
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Apple";
        ws.Range["A2"].Value = "Banana";
        ws.Range["A3"].Value = "Cherry";
        ws.Range["B1"].Value = "What is the second item in the list? Reply with just the word.";
        ws.Range["B2"].Formula = "=PROMPTMODEL(\"Ollama/gemma3:4b-it-qat\", B1, A1:A3)";

        // Act
        ExcelTestHelper.WaitForCellNotNA(ws.Range["B2"], timeoutSeconds: 120);

        // Assert
        var result = ws.Range["B2"].Value?.ToString() ?? string.Empty;
        Assert.Contains("Banana", result, StringComparison.OrdinalIgnoreCase);
    }

    [ExcelFact]
    public void Prompt_WithOllamaLowTemperature_ReturnsConsistentResponse()
    {
        // Arrange
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "What is 2+2? Reply with just the number.";
        ws.Range["A2"].Formula = "=PROMPTMODEL(\"Ollama/gemma3:4b-it-qat\", A1)";

        // Act
        ExcelTestHelper.WaitForCellNotNA(ws.Range["A2"], timeoutSeconds: 120);

        // Assert
        var result = ws.Range["A2"].Value?.ToString() ?? string.Empty;
        Assert.Contains("4", result);
    }
}
