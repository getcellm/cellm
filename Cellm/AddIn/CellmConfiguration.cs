using Cellm.ModelProviders;

namespace Cellm.AddIn;

public class CellmConfiguration
{
    public string DefaultModelProvider { get; init; }
    public double DefaultTemperature { get; init; }

    public int MaxTokens { get; init; }

    public CellmConfiguration()
    {
        DefaultModelProvider = string.Empty;
        DefaultTemperature = default;
        MaxTokens = default;
    }
}
