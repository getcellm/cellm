namespace Cellm.Prompts;

public enum Role
{
    System,
    User,
    Assistant
}

public record Message(string Content, Role Role);

public record Prompt(string SystemMessage, List<Message> messages, double Temperature);
