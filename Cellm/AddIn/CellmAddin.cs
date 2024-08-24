using ExcelDna.Integration;

namespace Cellm.AddIn;

public class CellmAddin : IExcelAddIn
{
    public string ApiKey { get; init; } = "API_KEY";
    public string ApiUrl { get; init; } = "https://api.anthropic.com/v1/messages";

    public void AutoOpen()
    {
    }

    public void AutoClose() 
    {
    }
}
