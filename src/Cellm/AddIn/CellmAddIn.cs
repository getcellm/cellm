using Cellm.Services;
using ExcelDna.Integration;

namespace Cellm.AddIn;

public class CellmAddIn : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(obj =>
        {
            var ex = (Exception)obj;
            SentrySdk.CaptureException(ex);
            return ex.Message;
        });

        _ = ServiceLocator.ServiceProvider;
    }

    public void AutoClose()
    {
        ServiceLocator.Dispose();
        SentrySdk.Flush();
    }
}
