namespace Cellm.Prompts;

public enum Role
{
    System,
    User,
    Assistant,
    Tool
}

public record ToolCall(string Id, string Type, Dictionary<string, string> Function);

public record Message(string Content, Role Role, List<ToolCall>? ToolCalls);

public record Prompt(string SystemMessage, List<Message> Messages, double Temperature);
