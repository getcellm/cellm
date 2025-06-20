using Microsoft.Extensions.AI;

namespace Cellm.Models.Prompts;

internal record Prompt(IList<ChatMessage> Messages, ChatOptions Options, StructuredOutputShape OutputShape);
