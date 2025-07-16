using Cellm.AddIn.Exceptions;
using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Behaviors;

/// <summary>
/// Shows pretty error message when provider does not support structured output with tools.
/// </summary>
/// <param name="providerConfigurations"></param>
internal class StructuredOutputWithToolsBehavior(IEnumerable<IProviderConfiguration> providerConfigurations) : IProviderBehavior
{
    public bool IsEnabled(Provider provider)
    {
        return !providerConfigurations.Single(x => x.Id == provider).CanUseStructuredOutputWithTools;
    }

    public void Before(Prompt prompt)
    {
        var isStructuredOutputEnabled = prompt.OutputShape != StructuredOutputShape.None;
        var isToolsEnabled = prompt.Options.Tools?.Any() ?? false;

        if (isStructuredOutputEnabled && isToolsEnabled)
        {
            throw new CellmException("Provider does not support row, column, or dynamic output while tools are enabled. " +
                "Please use PROMPT.TOCELL, disable tools, or switch to a provider that supports structured output with tools.");
        }
    }

    // No-op
    public void After(Prompt prompt) { }

    public UInt32 Order => 1;
}
