using Cellm.AddIn.Exceptions;

namespace Cellm.Users.Exceptions;

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
            Entitlement.EnableAnthropicProvider => $"Sign in and subscribe to use Anthropic models.",
            Entitlement.EnableAwsProvider => $"Sign in and subscribe to use models hosted by Azure.",
            Entitlement.EnableAzureProvider => $"Sign in and subscribe to use models hosted by Azure.",
            Entitlement.EnableCellmProvider => $"Sign in and subscribe to use models hosted by Cellm.",
            Entitlement.EnableDeepSeekProvider => $"Sign in and subscribe to use models hosted by DeepSeek.",
            Entitlement.EnableGeminiProvider => $"Sign in and subscribe to use models hosted by Azure.",
            Entitlement.EnableMistralProvider => $"Sign in and subscribe to use models hosted by Mistral.",
            Entitlement.EnableOllamaProvider => $"Sign in and subscribe to use models served by Ollama.",
            Entitlement.EnableOpenAiProvider => $"Sign in and subscribe to use models hosted by OpenAi.",
            Entitlement.EnableOpenAiCompatibleProvider => $"Sign in and subscribe to use OpenAI-compatible APIs.",
            _ => $"Unexpected error: You do not have the required permission \"{entitlement}\"",
        };
    }
}