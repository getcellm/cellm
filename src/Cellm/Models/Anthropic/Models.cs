using System.Text.Json.Serialization;

namespace Cellm.Models.Anthropic.Models;

public class AnthropicResponseBody
{
    public List<AnthropicContent>? Content { get; set; }

    public string? Id { get; set; }

    public string? Model { get; set; }

    public string? Role { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    public string? Type { get; set; }

    public AnthropicUsage? Usage { get; set; }
}

public class AnthropicRequestBody
{
    public List<AnthropicMessage>? Messages { get; set; }

    public string? System { get; set; }

    public string? Model { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    public double Temperature { get; set; }
}

public class AnthropicMessage
{
    public string? Role { get; set; }

    public string? Content { get; set; }
}

public class AnthropicContent
{
    public string? Text { get; set; }

    public string? Type { get; set; }
}

public class AnthropicUsage
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }
}