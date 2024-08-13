using System.Text;
using System.Text.Json.Serialization;
using ExcelDna.Integration;

namespace Cellm;

public enum Role
{
    System,
    User,
    Assistant
}

public record Message(string Content, Role Role);

public record Prompt(string SystemMessage, List<Message> messages, double Temperature);

public class PromptBuilder
{
    private string _systemMessage = string.Empty;
    private List<Message> _messages = new();
    private double _temperature = 0;

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
        return new Prompt(_systemMessage, _messages, _temperature);
    }
}
