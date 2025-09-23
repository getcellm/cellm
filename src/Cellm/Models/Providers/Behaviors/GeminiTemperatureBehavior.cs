using Cellm.AddIn;
using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Behaviors;

internal class GeminiTemperatureBehavior(IOptionsMonitor<CellmAddInConfiguration> cellmAddinConfiguration) : IProviderBehavior
{
    private const float DefaultMinTemp = 0.0f;
    private const float DefaultMaxTemp = 1.0f;
    private const float GeminiMaxTemperature = 2.0f;

    public bool IsEnabled(Provider provider)
    {
        return provider == Provider.Gemini;
    }

    public void Before(Provider provider, Prompt prompt)
    {
        var temperature = prompt.Options.Temperature ?? (float)cellmAddinConfiguration.CurrentValue.DefaultTemperature;

        // Scale temperature from [0;1] to [0;2]
        temperature = (temperature / DefaultMaxTemp) * GeminiMaxTemperature;
        prompt.Options.Temperature = Math.Clamp(temperature, DefaultMinTemp, GeminiMaxTemperature);
    }

    public void After(Provider Provider, Prompt prompt)
    {
        if (prompt.Options.Temperature.HasValue)
        {
            var temperature = prompt.Options.Temperature.Value;

            // Scale temperature back from [0;2] to [0;1]
            temperature = (temperature / GeminiMaxTemperature) * DefaultMaxTemp;
            prompt.Options.Temperature = Math.Clamp(temperature, DefaultMinTemp, DefaultMaxTemp);
        }
    }

    public UInt32 Order => 10;
}
