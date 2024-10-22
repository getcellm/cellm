using Cellm.AddIn.Exceptions;

namespace Cellm.Prompts;

public class PromptBuilder
{
    private string? _model;
    private string? _systemMessage;
    private List<Message> _messages = new();
    private double? _temperature;
    private List<Tool> _tools = new();

    public PromptBuilder()
    {
    }

    public PromptBuilder(Prompt prompt)
    {
        _model = prompt.Model;
        _systemMessage = prompt.SystemMessage;
        _messages = prompt.Messages;
        _temperature = prompt.Temperature;
    }

    public PromptBuilder SetModel(string model)
    {
        _model = model;
        return this;
    }

    public PromptBuilder SetSystemMessage(string systemMessage)
    {
        _systemMessage = systemMessage;
        return this;
    }

    public PromptBuilder SetTemperature(double temperature)
    {
        _temperature = temperature;
        return this;
    }

    public PromptBuilder AddSystemMessage()
    {
        if (string.IsNullOrEmpty(_systemMessage))
        {
            throw new CellmException("Cannot add empty system message");
        }

        _messages.Insert(0, new Message(_systemMessage!, Roles.System));
        return this;
    }

    public PromptBuilder AddSystemMessage(string content)
    {
        _messages.Add(new Message(content, Roles.System));
        return this;
    }

    public PromptBuilder AddUserMessage(string content)
    {
        _messages.Add(new Message(content, Roles.User));
        return this;
    }

    public PromptBuilder AddAssistantMessage(string content, List<ToolCall>? toolCalls = null)
    {
        _messages.Add(new Message(content, Roles.Assistant, toolCalls));
        return this;
    }

    public PromptBuilder AddMessage(Message message)
    {
        return AddMessages(new List<Message> { message });
    }

    public PromptBuilder AddMessages(List<Message> messages)
    {
        _messages.AddRange(messages);
        return this;
    }

    public PromptBuilder AddTools(List<Tool> tools)
    {
        _tools = tools;
        return this;
    }

    public Prompt Build()
    {
        return new Prompt(
            _model ?? throw new ArgumentNullException(nameof(_model)),
            _systemMessage ?? string.Empty,
            _messages,
            _temperature ?? throw new ArgumentNullException(nameof(_temperature)),
            _tools
        );
    }
}
