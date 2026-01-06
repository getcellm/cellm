using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;
using Xunit;

namespace Cellm.Tests.Integration;

[ExcelTestSettings(AddIn = @"..\..\..\..\Cellm\bin\Debug\net9.0-windows\Cellm-AddIn")]
[Trait("Category", "Excel")]
public class ExcelSanityTests : IDisposable
{
    private readonly Workbook _testWorkbook;

    public ExcelSanityTests()
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
    public void TestSanity()
    {
        Worksheet ws = (Worksheet)_testWorkbook.Sheets[1];
        ws.Range["A1"].Value = 3;
        ws.Range["A2"].Value = 5;
        ws.Range["A3"].Formula = "=A1+A2";
        Assert.Equal(8, ws.Range["A3"].Value);
    }
}
