namespace Cellm.Mcp;

public record PromptResponse(
    int Version,
    string Status,
    string Workbook,
    string Worksheet,
    string Cell,
    string? Formula = null,
    object? Value = null,
    string? Error = null);
