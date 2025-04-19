using Cellm.AddIn.Exceptions;

namespace Cellm.User.Exceptions;

internal class PermissionDeniedException : CellmException
{
    public PermissionDeniedException(Entitlement entitlement)
        : base(GetMessage(entitlement)) { }

    public PermissionDeniedException(Entitlement entitlement, Exception inner)
        : base(GetMessage(entitlement), inner) { }

    private static string GetMessage(Entitlement entitlement)
    {
        return entitlement switch
        {
            Entitlement.EnableAnthropicProvider => $"You do not have permission to use models hosted by Anthropic.",
            Entitlement.EnableCellmProvider => $"You do not have permission to use models hosted by Cellm.",
            Entitlement.EnableDeepSeekProvider => $"You do not have permission to use models hosted by DeepSeek.",
            Entitlement.EnableMistralProvider => $"You do not have permission to use models hosted by Mistral.",
            Entitlement.EnableOllamaProvider => $"You do not have permission to use models served by Ollama.",
            Entitlement.EnableOpenAiProvider => $"You do not have permission to use models hosted by OpenAi.",
            Entitlement.EnableOpenAiCompatibleProvider => $"You do not have permission to use OpenAI-compatible APIs.",
            _ => $"Unexpected error: You do not have the required permission \"{entitlement}\"",
        };
    }
}