using Microsoft.Extensions.AI;

namespace Cellm.Models.Prompts;

public record Prompt(IList<ChatMessage> Messages, ChatOptions Options);
