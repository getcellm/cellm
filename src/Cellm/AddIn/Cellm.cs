using Cellm.Services;
using ExcelDna.Integration;

namespace Cellm.AddIn;

public class Cellm : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(obj =>
        {
            var ex = (Exception)obj;
            SentrySdk.CaptureException(ex);
            return ex.Message;
        });
    }

    public void AutoClose()
    {
        ServiceLocator.Dispose();
        SentrySdk.Flush();
    }
}
