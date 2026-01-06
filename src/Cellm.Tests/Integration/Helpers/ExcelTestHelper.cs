using ExcelDna.Testing;
using Microsoft.Office.Interop.Excel;

namespace Cellm.Tests.Integration.Helpers;

public static class ExcelTestHelper
{
    // Excel error codes
    private const int XlErrNA = -2146826245;      // #N/A
    private const int XlErrGettingData = -2146826245; // #GETTING_DATA displays as #N/A in some cases

    public static void WaitForCellValue(Microsoft.Office.Interop.Excel.Range cell, string expectedValue, int timeoutSeconds = 30)
    {
        Automation.WaitFor(() => cell.Value?.ToString() == expectedValue, timeoutSeconds * 1000);
    }

    public static void WaitForCellNotNA(Microsoft.Office.Interop.Excel.Range cell, int timeoutSeconds = 30)
    {
        var worksheet = cell.Worksheet;
        var application = worksheet.Application;

        Automation.WaitFor(() =>
        {
            // Force Excel to recalculate and process pending RTD updates
            try
            {
                application.CalculateFull();
            }
            catch
            {
                // Ignore calculation errors
            }

            var value = cell.Value;

            // Check for #N/A error code (returned as int) or #N/A string
            if (value is int intValue && intValue == XlErrNA)
            {
                return false;
            }

            if (value?.ToString() == "#N/A")
            {
                return false;
            }

            // Check for #GETTING_DATA error
            if (value is ExcelDna.Integration.ExcelError)
            {
                return false;
            }

            return value != null;
        }, timeoutSeconds * 1000);
    }
}
