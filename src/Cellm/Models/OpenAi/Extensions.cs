using Cellm.Prompts;
using Cellm.Tools;

namespace Cellm.Models.OpenAi;

internal static class Extensions
{
    public static List<OpenAiMessage> ToOpenAiMessages(this Prompt prompt)
    {
        return prompt.Messages.SelectMany(x => ToOpenAiMessage(x)).ToList();
    }

    private static List<OpenAiMessage> ToOpenAiMessage(Message message)
    {
        return message.Role switch {
            Roles.Tool => ToOpenAiToolResults(message),
            _ => new List<OpenAiMessage>() 
            {
                new OpenAiMessage
                {
                    Role = message.Role.ToString().ToLower(),
                    Content = message.Content,
                    ToolCalls = message.ToolCalls?.Select(x => new OpenAiToolCall
                    {
                        Id = x.Id,
                        Type = "function",
                        Function = new OpenAiFunctionCall
                        {
                            Name = x.Name,
                            Arguments = x.Arguments
                        }
                    }).ToList()
                }
            }
        };
    }

    private static List<OpenAiMessage> ToOpenAiToolResults(Message message)
    {
        return message.ToolCalls.Select(x => new OpenAiMessage
        {
            ToolCallId = x.Id,
            Role = Roles.Tool.ToString().ToLower(),
            Content = $"Tool: {x.Name}, Arguments: {x.Arguments}, Result: {x.Result}"
        }).ToList();
    }

    public static List<OpenAiTool> ToOpenAiTools(this ITools tools)
    {
        return tools.GetTools()
            .Select(x => new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = x.Name,
                    Description = x.Description,
                    Parameters = x.Parameters
                }
            })
            .ToList();
    }
}
