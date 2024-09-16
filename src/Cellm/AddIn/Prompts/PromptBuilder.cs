using System.Text;

namespace Cellm.AddIn.Prompts;

public class PromptBuilder
{
    private string? _systemMessage;
    private readonly List<Message> _messages = new();
    private double? _temperature;

    public PromptBuilder()
    {
    }

    public PromptBuilder(Prompt prompt)
    {
        _systemMessage = prompt.SystemMessage;
        _messages = prompt.Messages;
        _temperature = prompt.Temperature;
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

    public PromptBuilder AddUserMessage(string content)
    {
        _messages.Add(new Message(content, Role.User));
        return this;
    }

    public PromptBuilder AddAssistantMessage(string content)
    {
        _messages.Add(new Message(content, Role.Assistant));
        return this;
    }

    public PromptBuilder AddMessages(List<Message> messages)
    {
        _messages.AddRange(messages);
        return this;
    }

    public Prompt Build()
    {
        return new Prompt(
            _systemMessage ?? throw new ArgumentNullException(nameof(_systemMessage)),
            _messages,
            _temperature ?? throw new ArgumentNullException(nameof(_temperature))
        );
    }
}
