namespace Cellm.Prompts;

public record Tool(string Name, string Description, Dictionary<string, (string Description, string Type)> Parameters);
