using Microsoft.Extensions.AI;

namespace Cellm.Prompts;

public record Prompt(IList<ChatMessage> Messages, ChatOptions Options);
