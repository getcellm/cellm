using System.Text.Json;

namespace Cellm.Models.OpenAi.Models;

public record OpenAiChatCompletionRequest(
    string Model,
    List<OpenAiMessage> Messages,
    int MaxTokens,
    double Temperature,
    List<OpenAiTool>? Tools = null,
    string? ToolChoice = null);

public record OpenAiChatCompletionResponse(
   string Id,
   string Object,
   long Created,
   string Model,
   List<OpenAiChoice> Choices,
   OpenAiUsage? Usage = null);

public record OpenAiMessage(
   string Role,
   string Content,
   List<OpenAiToolCall>? ToolCalls = null,
   string? ToolCallId = null);

public record OpenAiTool(
    string Type,
    OpenAiFunction Function);

public record OpenAiFunction(
    string Name,
    string Description,
    JsonDocument Parameters);

public record OpenAiChoice(
    int Index,
    OpenAiMessage Message,
    string FinishReason);

public record OpenAiToolCall(
    string Id,
    string Type,
    OpenAiFunctionCall Function);

public record OpenAiFunctionCall(
    string Name,
    string Arguments);

public record OpenAiUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);
