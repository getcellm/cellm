namespace Cellm.Prompts;

public record ToolCall(string Id, string Name, string Arguments, string? Result);