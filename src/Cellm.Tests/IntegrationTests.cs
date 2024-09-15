using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests;

[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net6.0-windows\Cellm-AddIn")]
public class ExcelTests : IDisposable
{
    readonly Workbook _testWorkbook;

    public ExcelTests()
    {
        var app = Util.Application;
        _testWorkbook = app.Workbooks.Add();
    }

    public void Dispose()
    {
        _testWorkbook.Close(SaveChanges: false);
    }

    [ExcelFact]
    public void TestSanity()
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = 3;
        ws.Range["A2"].Value = 5;
        ws.Range["A3"].Formula = "=A1+A2";
        Assert.Equal(8, ws.Range["A3"].Value);
    }

    [ExcelFact]
    public void TestPrompt()
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Respond with \"Hello World\"";
        ws.Range["A2"].Formula = "=PROMPT(A1)";
        ExcelTestHelper.WaitForCellValue(ws.Range["A2"]);
        Assert.Equal("Hello World", ws.Range["A2"].Text);

        ws.Range["A3"].Formula = "=PROMPT(A1, \"Respond with \"Hi\")";
        ExcelTestHelper.WaitForCellValue(ws.Range["A3"]);
        Assert.Equal("Hi", ws.Range["A3"].Text);

        ws.Range["A4"].Formula = "=PROMPT(A1, \"Respond with \"Hi\", 0.2)";
        ExcelTestHelper.WaitForCellValue(ws.Range["A4"]);
        Assert.Equal("Hi", ws.Range["A4"].Text);
    }

    [ExcelFact]
    public void TestPromptWith()
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = "Respond with \"Hello World\"";
        ws.Range["A2"].Formula = "=PROMPTWITH(\"Anthropic/claude-3-haiku-20240307\",A1)";
        ExcelTestHelper.WaitForCellValue(ws.Range["A2"]);
        Assert.Equal("Hello World", ws.Range["A2"].Text);

        ws.Range["B1"].Value = "Respond with \"Hello World\"";
        ws.Range["B2"].Formula = "=PROMPTWITH(\"OpenAI/gpt-4o-mini\",B1)";
        ExcelTestHelper.WaitForCellValue(ws.Range["B2"]);
        Assert.Equal("Hello World", ws.Range["B2"].Text);

        ws.Range["C1"].Value = "Respond with \"Hello World\"";
        ws.Range["C2"].Formula = "=PROMPTWITH(\"OpenAI/gemini-1.5-flash-latest\",C1)";
        ExcelTestHelper.WaitForCellValue(ws.Range["C2"]);
        Assert.Equal("Hello World", ws.Range["C2"].Text);
    }
}

public static class ExcelTestHelper
{
    public static void WaitForCellValue(Microsoft.Office.Interop.Excel.Range cell, int timeoutSeconds = 30)
    {
        DateTime start = DateTime.Now;
        while (DateTime.Now - start < TimeSpan.FromSeconds(timeoutSeconds))
        {
            if (cell.Text != "#N/A")
            {
                return;
            }

            Thread.Sleep(100);
        }
        throw new TimeoutException($"Cell value not updated within {timeoutSeconds} seconds");
    }
}
