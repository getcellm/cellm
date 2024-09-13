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

        ws.Range["A1"].Value = 2;
        ws.Range["A2"].Value = 3;
        ws.Range["A3"].Formula = "=A1+A2";

        var result = ws.Range["A3"].Value;

        Assert.Equal(5, result);
    }
}