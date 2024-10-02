namespace Cellm.Prompts;

public record Prompt(string SystemMessage, List<Message> Messages, double Temperature);
