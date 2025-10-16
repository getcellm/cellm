using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Behaviors;

internal class OpenAiTemperatureBehavior() : IProviderBehavior
{
    private const float Gpt5Temperature = 1f;

    public bool IsEnabled(Provider provider)
    {
        return provider is Provider.OpenAi or Provider.OpenAiCompatible;
    }

    public void Before(Provider provider, Prompt prompt)
    {
        if (prompt.Options.ModelId?.StartsWith("gpt-5") ?? false)
        {
            prompt.Options.Temperature = Gpt5Temperature;
        }
    }

    // No-op
    public void After(Provider Provider, Prompt prompt) { }

    public UInt32 Order => 40;
}
