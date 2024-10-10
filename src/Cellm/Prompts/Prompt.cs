namespace Cellm.Prompts;

public record Prompt(string Model, string SystemMessage, List<Message> Messages, double Temperature, List<Tool> Tools);
