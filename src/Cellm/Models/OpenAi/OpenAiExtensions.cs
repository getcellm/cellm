using System.Text.Json.Serialization;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Cellm.Tools;

namespace Cellm.Models.OpenAi;

internal static class OpenAiExtensions
{
    public static List<Message> ToOpenAiMessages(this Prompt prompt)
    {
        return prompt.Messages.SelectMany(x => ToOpenAiMessage(x)).ToList();
    }

    private static List<Message> ToOpenAiMessage(Prompts.Message message)
    {
        return message.Role switch
        {
            Roles.Tool => message?.ToolRequests?
                .Select(x => new Message
                {
                    Content = $"Tool: {x.Name}, Arguments: {x.Arguments}, Result: {x.Response}",
                    Role = message.Role.ToString().ToLower(),
                    ToolCallId = x.Id
                })
                .ToList() ?? throw new CellmException(),
            _ => new List<Message>
            {
                new Message {
                    Content = message.Content,
                    Role = message.Role.ToString().ToLower(),
                    ToolCallId = null
                }
            },
        };
    }

    public static List<OpenAiTool> ToOpenAiTools(this ITools tools)
    {
        return tools.GetTools()
            .Select(x => new OpenAiTool
            {
                Function = new Function
                {
                    Name = x.Name,
                    Description = x.Description,
                    Parameters = new Parameters
                    {
                        Properties = x.Parameters.ToDictionary(
                        y => y.Key,
                        y => new Property
                        {
                            Type = y.Value.Type,
                            Description = y.Value.Description
                        }
                    ),
                        AdditionalProperties = false
                    }
                }
            })
            .ToList();
    }
}

internal class RequestBody
{
    public string? Model { get; set; }

    public List<Message>? Messages { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; }

    public double Temperature { get; set; }

    public List<ToolCall>? Tools { get; set; }
}

internal class ResponseBody
{
    public string? Id { get; set; }

    public string? Object { get; set; }

    public long? Created { get; set; }

    public string? Model { get; set; }

    public List<Choice>? Choices { get; set; }

    public Usage? Usage { get; set; }

    public string? SystemFingerprint { get; set; }
}

internal class Message
{
    public string? Role { get; set; }

    public string? Content { get; set; }

    public List<ToolCall>? ToolCalls { get; set; }

    public string? ToolCallId { get; set; }

    public object? Refusal { get; set; }

}

internal class Choice
{
    public int Index { get; set; }

    public Message? Message { get; set; }

    public object? Logprobs { get; set; }

    public string? FinishReason { get; set; }
}

internal class Usage
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}


public class ToolCall
{
    public string Id { get; set; }

    public string Type { get; set; }

    public FunctionCall Function { get; set; }
}

public class FunctionCall
{
    public string Name { get; set; }

    public string Arguments { get; set; }
}

public class Usage
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public PromptTokensDetails PromptTokensDetails { get; set; }

    public CompletionTokensDetails CompletionTokensDetails { get; set; }
}

public class PromptTokensDetails
{
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }
}

public class CompletionTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int ReasoningTokens { get; set; }
}

