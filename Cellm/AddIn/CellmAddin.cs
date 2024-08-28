using ExcelDna.Integration;

namespace Cellm.AddIn;

public class CellmAddin : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(ex => ex.ToString());
    }

    public void AutoClose()
    {
    }
}
