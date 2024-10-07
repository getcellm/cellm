using System.Text.Json.Serialization;
using Cellm.Prompts;

namespace Cellm.Models.OpenAi;

internal static class Convert
{
    public static List<Message> Messages(this Prompt prompt)
    {
        return new List<Message>();
    }

    public static PromptBuilder AddAssistantMessage(this PromptBuilder promptBuilder, Choice choice)
    {
        return promptBuilder;
    }
}

internal class RequestBody
{
    public string? Model { get; set; }

    public List<Message>? Messages { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; }

    public double Temperature { get; set; }

    public List<OpenAiTool>? Tools { get; set; }
}

internal class ResponseBody
{
    public string? Id { get; set; }

    public string? Object { get; set; }

    public long Created { get; set; }

    public string? Model { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    public List<Choice>? Choices { get; set; }

    public Usage? Usage { get; set; }
}

internal class Message
{
    public string? Role { get; set; }

    public string? Content { get; set; }

    public string? ToolCallId { get; set; }

    public List<OpenAiTool>? ToolCalls { get; set; }
}

internal class Choice
{
    public int Index { get; set; }

    public Message? Message { get; set; }

    public object? Logprobs { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAiTool>? ToolCalls;
}

internal class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}


internal class OpenAiTool
{
    public string? Id;

    public string Type { get; set; } = "function";

    public Function? Function { get; set; }
}

internal class Function
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public Parameters? Parameters { get; set; }

    public string? Arguments { get; set; }
}

internal class Parameters
{
    public string? Type { get; set; }

    public Dictionary<string, Property>? Properties { get; set; }

    public List<string>? Required { get; set; }

    public bool? AdditionalProperties { get; set; }
}

internal class Property
{
    public string? Type { get; set; }

    public string? Description { get; set; }
}

