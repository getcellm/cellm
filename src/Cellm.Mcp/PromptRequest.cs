namespace Cellm.Mcp;

internal record PromptRequest(
    int Version,
    string Method,
    string Workbook,
    string Worksheet,
    string Cell,
    string Prompt,
    IReadOnlyList<string> Ranges,
    string? ProviderAndModel,
    bool Overwrite);
