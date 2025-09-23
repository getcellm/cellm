using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Behaviors;

internal interface IProviderBehavior
{
    internal bool IsEnabled(Provider provider);

    internal void Before(Provider provider, Prompt prompt);

    internal void After(Provider provider, Prompt prompt);

    internal UInt32 Order { get; }
}
