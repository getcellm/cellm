using System.Diagnostics;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Behaviors;

/// <summary>
/// Shows pretty error message when provider does not support structured output with tools.
/// </summary>
/// <param name="providerConfigurations"></param>
internal class StructuredOutputWithToolsBehavior : IProviderBehavior
{
    public bool IsEnabled(Provider provider)
    {

        return !CellmAddIn
            .GetProviderConfigurations()
            .Single(x => x.Id == provider)
            .CanUseStructuredOutputWithTools;
    }

    public void Before(Provider Provider, Prompt prompt)
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
    public void After(Provider Provider, Prompt prompt) { }

    public UInt32 Order => 0;
}
