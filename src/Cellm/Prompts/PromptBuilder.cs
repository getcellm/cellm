using Cellm.AddIn.Exceptions;
using Microsoft.Extensions.AI;

namespace Cellm.Prompts;

public class PromptBuilder
{
    private List<ChatMessage> _messages = new();
    private ChatOptions _options = new();
    private List<AIFunction> _tools = new();

    public PromptBuilder()
    {
    }

    public PromptBuilder(Prompt prompt)
    {
        // Do not mutate prompt
        _messages = new List<ChatMessage>(prompt.Messages);
        _options = prompt.Options.Clone();
    }

    public PromptBuilder SetModel(string model)
    {
        _options.ModelId = model;
        return this;
    }

    public PromptBuilder SetTemperature(double temperature)
    {
        _options.Temperature = (float)temperature;
        return this;
    }

    public PromptBuilder AddSystemMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.System, content));
        return this;
    }

    public PromptBuilder AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
        return this;
    }

    public PromptBuilder AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
        return this;
    }

    public PromptBuilder AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        return this;
    }

    public PromptBuilder AddMessages(List<ChatMessage> messages)
    {
        _messages.AddRange(messages);
        return this;
    }

    public PromptBuilder SetTools(IList<AITool> tools)
    {
        _options.Tools = tools;
        return this;
    }

    public Prompt Build()
    {
        return new Prompt(_messages, _options);
    }
}
