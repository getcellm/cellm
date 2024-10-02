namespace Cellm.Prompts;

public record ToolRequest(string Id, string Name, string Description, Dictionary<string, string> Arguments, string? Response);
