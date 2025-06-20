using Microsoft.Extensions.AI;

namespace Cellm.Models.Prompts;

internal class PromptBuilder
{
    private readonly List<ChatMessage> _messages = [];
    private readonly ChatOptions _options = new();
    private StructuredOutputShape _outputShape = StructuredOutputShape.None;

    internal PromptBuilder()
    {
    }

    internal PromptBuilder(Prompt prompt)
    {
        // Do not mutate prompt
        _messages = new List<ChatMessage>(prompt.Messages);
        _options = prompt.Options.Clone();
        _outputShape = prompt.OutputShape;
    }

    internal PromptBuilder SetModel(string model)
    {
        _options.ModelId = model;
        return this;
    }

    internal PromptBuilder SetTemperature(double temperature)
    {
        _options.Temperature = (float)temperature;
        return this;
    }

    internal PromptBuilder SetMaxOutputTokens(int maxOutputTokens)
    {
        _options.MaxOutputTokens = maxOutputTokens;
        return this;
    }

    internal PromptBuilder SetOutputShape(StructuredOutputShape outputShape)
    {
        _outputShape = outputShape;
        return this;
    }

    internal PromptBuilder AddSystemMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.System, content));
        return this;
    }

    internal PromptBuilder AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
        return this;
    }

    internal PromptBuilder AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
        return this;
    }

    internal PromptBuilder AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        return this;
    }

    internal PromptBuilder AddMessages(IList<ChatMessage> messages)
    {
        _messages.AddRange(messages);
        return this;
    }

    internal PromptBuilder SetTools(IList<AITool> tools)
    {
        _options.Tools = tools;
        return this;
    }

    internal Prompt Build()
    {
        return new Prompt(_messages, _options, _outputShape);
    }
}
