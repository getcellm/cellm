using Cellm.AddIn;
using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Behaviors;

internal class AdditionalPropertiesBehavior : IProviderBehavior
{
    bool IProviderBehavior.IsEnabled(Provider Provider)
    {
        return true;
    }

    void IProviderBehavior.Before(Provider provider, Prompt prompt)
    {
        var providerConfiguration = CellmAddIn.GetProviderConfigurations().Single(x => x.Id == provider);

        if (providerConfiguration.AdditionalProperties is null)
        {
            return;
        }

        prompt.Options.AdditionalProperties = providerConfiguration.AdditionalProperties;
    }

    // No-op
    void IProviderBehavior.After(Provider Provider, Prompt prompt) { }


    UInt32 IProviderBehavior.Order => 20;
}
