using Cellm.Tests.Integration.Helpers;
using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests.Integration;

/// Exercises the full Excel path: =PROMPTMODEL → Excel → AddIn → MediatR pipeline → Provider.
/// Complements ProviderTests which test provider code in isolation via DI.
/// If ProviderTests pass but these fail, the problem is between Excel and the provider code.
[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net9.0-windows\Cellm-AddIn")]
[Trait("Category", "Excel")]
[Trait("Category", "Integration")]
public class ExcelProviderTests : IDisposable
{
    private readonly Workbook _testWorkbook;

    public ExcelProviderTests()
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
    public void BasicPrompt_Anthropic_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("Anthropic/claude-sonnet-4-6", "4");
    }

    [ExcelFact]
    public void BasicPrompt_DeepSeek_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("DeepSeek/deepseek-chat", "4");
    }

    [ExcelFact]
    public void BasicPrompt_Gemini_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("Gemini/gemini-3.1-flash-lite-preview", "4");
    }

    [ExcelFact]
    public void BasicPrompt_Mistral_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("Mistral/mistral-small-latest", "4");
    }

    [ExcelFact]
    public void BasicPrompt_Ollama_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("Ollama/gemma4:e4b", "4");
    }

    [ExcelFact]
    public void BasicPrompt_OpenAi_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("OpenAi/gpt-5.4-nano", "4");
    }

    [ExcelFact]
    public void BasicPrompt_OpenRouter_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("OpenRouter/mistralai/mistral-small-3.2-24b-instruct", "4");
    }

    [ExcelFact]
    public void ThinkingModel_Mistral_Magistral_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("Mistral/magistral-small-2509", "4");
    }

    [ExcelFact]
    public void ThinkingModel_DeepSeek_Reasoner_ReturnsResponse()
    {
        AssertPromptModelReturnsResponse("DeepSeek/deepseek-reasoner", "4");
    }

    private void AssertPromptModelReturnsResponse(string providerAndModel, string expectedSubstring)
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];

        // Use unique cell addresses per call to avoid cache collisions
        var row = ws.UsedRange.Rows.Count + 1;
        var instructionCell = $"A{row}";
        var resultCell = $"B{row}";

        ws.Range[instructionCell].Value = "What is 2+2? Reply with just the number.";
        ws.Range[resultCell].Formula = $"=PROMPTMODEL(\"{providerAndModel}\", {instructionCell})";

        ExcelTestHelper.WaitForCellNotNA(ws.Range[resultCell], timeoutSeconds: 120);

        var result = ws.Range[resultCell].Value?.ToString() ?? string.Empty;
        Assert.Contains(expectedSubstring, result);
    }
}
