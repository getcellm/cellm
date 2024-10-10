using Sentry.Protocol;

namespace Cellm.Prompts;

public record ToolRequest(string Id, string Name, Dictionary<string, string> Arguments, string? Response);
