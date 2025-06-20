using Cellm.Models.Prompts;

namespace Cellm.Models.Behaviors;

internal interface IGetPrompt
{
    public Prompt Prompt { get; }
}
