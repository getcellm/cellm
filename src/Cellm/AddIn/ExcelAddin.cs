using Cellm.Services;
using ExcelDna.Integration;

namespace Cellm.AddIn;

public class ExcelAddIn : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(obj =>
        {
            var e = (Exception)obj;
            SentrySdk.CaptureException(e);
            return e.Message;
        });

        _ = ServiceLocator.ServiceProvider;
    }

    public void AutoClose()
    {
        ServiceLocator.Dispose();
        SentrySdk.Flush();
    }
}
