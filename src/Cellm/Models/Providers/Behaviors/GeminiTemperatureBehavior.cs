using Cellm.AddIn;
using Cellm.Models.Behaviors;
using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Behaviors;

internal class GeminiTemperatureBehavior(IOptionsMonitor<CellmAddInConfiguration> cellmAddinConfiguration) : IProviderBehavior
{
    private const float DefaultMaxTemp = 1.0f;
    private const float GeminiMaxTemperature = 2.0f;

    public bool IsEnabled(Provider provider)
    {
        return provider == Provider.Gemini;
    }

    public void Before(Prompt prompt)
    {
        var temperature = prompt.Options.Temperature ?? (float)cellmAddinConfiguration.CurrentValue.DefaultTemperature;

        // Scale temperature from [0;1] to [0;2]
        temperature = (temperature / DefaultMaxTemp) * GeminiMaxTemperature;
        prompt.Options.Temperature = Math.Clamp(temperature, 0.0f, GeminiMaxTemperature);
    }

    // No-op
    public void After(Prompt prompt) { }

    public UInt32 Order => 10;
}
