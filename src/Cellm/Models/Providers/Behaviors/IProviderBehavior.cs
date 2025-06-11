using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Behaviors;

internal interface IProviderBehavior
{
    internal bool IsEnabled(Provider Provider);

    internal void Before(Prompt prompt);

    internal void After(Prompt prompt);

    internal UInt32 Order { get; }
}
