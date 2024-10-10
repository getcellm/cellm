using Cellm.Prompts;
using Cellm.Tools;

namespace Cellm.Models.OpenAi;

internal static class Extensions
{
    public static List<OpenAiMessage> ToOpenAiMessages(this Prompt prompt)
    {
        return prompt.Messages.Select(x => ToOpenAiMessage(x)).ToList();
    }

    private static OpenAiMessage ToOpenAiMessage(Prompts.Message message)
    {
        return new OpenAiMessage
        {
            Role = message.Role.ToString().ToLower(),
            Content = message.Content,
            ToolCalls = message.ToolRequests?.Select(x => new OpenAiToolCall
            {
                Id = x.Id,
                Type = "function",
                Function = new OpenAiFunctionCall
                {
                    Name = x.Name,
                    Arguments = System.Text.Json.JsonSerializer.Serialize(x.Arguments)
                }
            }).ToList()
        };
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
                    Parameters = new OpenAiParameters
                    {
                        Properties = x.Parameters.ToDictionary(
                            y => y.Key,
                            y => new OpenAiProperty
                            {
                                Type = y.Value.Type,
                                Description = y.Value.Description
                            }
                        ),
                        Required = new List<string>() // We don't have this information in the Tool class
                    }
                }
            })
            .ToList();
    }
}
