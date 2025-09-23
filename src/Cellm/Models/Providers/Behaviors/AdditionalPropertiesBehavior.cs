using Cellm.AddIn;
using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Behaviors;

internal class AdditionalPropertiesBehavior : IProviderBehavior
{
    bool IProviderBehavior.IsEnabled(Provider Provider)
    {
        return true;
    }

    void IProviderBehavior.Before(Provider provider, Prompt prompt)
    {
        if (prompt.Options.AdditionalProperties is null)
        {
            return;
        }

        var providerConfiguration = CellmAddIn.GetProviderConfigurations().Single(x => x.Id == provider);
        prompt.Options.AdditionalProperties = providerConfiguration.AdditionalProperties;
    }

    // No-op
    void IProviderBehavior.After(Provider Provider, Prompt prompt) { }


    UInt32 IProviderBehavior.Order => 20;
}
