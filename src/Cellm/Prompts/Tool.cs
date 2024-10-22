using System.Text.Json;

namespace Cellm.Prompts;

public record Tool(string Name, string Description, JsonDocument Parameters);
