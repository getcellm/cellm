# Cellm MCP server

This project exposes Cellm to MCP clients over standard input/output. Excel must be running with the Cellm add-in loaded.

For a development build, configure the client to run:

```json
{
  "command": "dotnet",
  "args": ["C:\\path\\to\\Cellm.Mcp.dll"]
}
```

The first tool, `cellm_prompt`, writes a normal `PROMPT` or `PROMPTMODEL` formula to an explicit workbook, worksheet, and cell. It then waits for Excel's result. Cancelling the MCP call stops waiting but does not replace Cellm's existing Excel cancellation behavior.
