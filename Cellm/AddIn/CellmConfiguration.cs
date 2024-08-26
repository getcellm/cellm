namespace Cellm.AddIn;

public class CellmConfiguration
{
    public string DefaultModelProvider { get; init; }

    public CellmConfiguration()
    {
        DefaultModelProvider = default!;
    }
}
