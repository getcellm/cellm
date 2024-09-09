using Cellm.Services;
using Cellm.Services.Telemetry;
using ExcelDna.Integration;

namespace Cellm.AddIn;

public class Cellm : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(ex => ex.ToString());

        ServiceLocator.Get<ITelemetry>().Start();
    }

    public void AutoClose()
    {
        ServiceLocator.Get<ITelemetry>().Stop();
    }
}
