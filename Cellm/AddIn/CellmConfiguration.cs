using Cellm.Models;

namespace Cellm.AddIn;

public class CellmConfiguration
{
    public string DefaultModelProvider { get; init; }
    public double DefaultTemperature { get; init; }

    public CellmConfiguration()
    {
        DefaultModelProvider = nameof(AnthropicClient);
        DefaultTemperature = 0.0;
    }
}
