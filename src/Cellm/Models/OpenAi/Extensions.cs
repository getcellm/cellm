using Cellm.AddIn.Exceptions;
using Cellm.Models.OpenAi.Models;
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
        return message.Role switch
        {
            Roles.Tool => ToOpenAiToolResults(message),
            _ => new List<OpenAiMessage>()
            {
                new OpenAiMessage(
                    message.Role.ToString().ToLower(),
                    message.Content,
                    message.ToolCalls?
                        .Select(x => new OpenAiToolCall(x.Id, "function", new OpenAiFunctionCall(x.Name, x.Arguments)))
                        .ToList())
            }
        };
    }

    private static List<OpenAiMessage> ToOpenAiToolResults(Message message)
    {
        var toolCalls = message?.ToolCalls ?? throw new CellmException("No tool calls in tool message");
        return toolCalls
            .Select(x => new OpenAiMessage(
                Roles.Tool.ToString().ToLower(),
                $"Tool: {x.Name}, Arguments: {x.Arguments}, Result: {x.Result}",
                null,
                x.Id)
            ).ToList();
    }

    public static List<OpenAiTool> ToOpenAiTools(this ToolRunner toolRunner)
    {
        return toolRunner.GetTools()
            .Select(x => new OpenAiTool("function", new OpenAiFunction(x.Name, x.Description, x.Parameters)))
            .ToList();
    }
}
