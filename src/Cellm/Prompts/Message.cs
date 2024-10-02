namespace Cellm.Prompts;

public record Message(string Content, Roles Role, List<ToolRequest>? ToolRequests = null);