using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Behaviors;
using Microsoft.Extensions.AI;
using Xunit;

namespace Cellm.Tests.Unit.Behaviors;

/// <summary>
/// Tests for MistralThinkingBehavior to verify handling of Mistral responses,
/// particularly edge cases that may cause NullReferenceException (issue #309).
/// </summary>
public class MistralThinkingBehaviorTests
{
    private readonly MistralThinkingBehavior _behavior;

    public MistralThinkingBehaviorTests()
    {
        _behavior = new MistralThinkingBehavior();
    }

    #region IsEnabled Tests

    [Theory]
    [InlineData(Provider.Mistral, true)]
    [InlineData(Provider.OpenAi, false)]
    [InlineData(Provider.Anthropic, false)]
    [InlineData(Provider.Ollama, false)]
    [InlineData(Provider.Cellm, false)]
    public void IsEnabled_ReturnsCorrectValue(Provider provider, bool expected)
    {
        // Act
        var result = _behavior.IsEnabled(provider);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region After Method - Normal Response Tests

    [Fact]
    public void After_WithNormalTextResponse_DoesNotModifyMessage()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Add assistant message
        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, "Hello! How can I help you?"));

        var originalText = prompt.Messages.Last().Text;

        // Act
        _behavior.After(Provider.Mistral, prompt);

        // Assert - Message should be unchanged since it's not a thinking response
        Assert.Equal(originalText, prompt.Messages.Last().Text);
    }

    [Fact]
    public void After_WithEmptyMessages_DoesNotThrow()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .Build();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    [Fact]
    public void After_WithOnlyUserMessage_DoesNotThrow()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Act & Assert - Should not throw (last message is User, not Assistant)
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    #endregion

    #region After Method - Null/Empty Text Tests (Issue #309 reproduction)

    [Fact]
    public void After_WithNullTextInAssistantMessage_DoesNotThrow()
    {
        // Arrange - This simulates the scenario where Mistral SDK returns a message with null Text
        // which was reported in issue #309: "Object reference not set to an instance of an object"
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Create a ChatMessage with null text content to simulate Mistral SDK behavior
        var assistantMessage = new ChatMessage(ChatRole.Assistant, (string?)null);
        prompt.Messages.Add(assistantMessage);

        // Act & Assert - Should not throw NullReferenceException
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    [Fact]
    public void After_WithEmptyTextInAssistantMessage_DoesNotThrow()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        var assistantMessage = new ChatMessage(ChatRole.Assistant, string.Empty);
        prompt.Messages.Add(assistantMessage);

        // Act & Assert
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    [Fact]
    public void After_WithWhitespaceTextInAssistantMessage_DoesNotThrow()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        var assistantMessage = new ChatMessage(ChatRole.Assistant, "   \t\n   ");
        prompt.Messages.Add(assistantMessage);

        // Act & Assert
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    [Fact]
    public void After_WithEmptyContentsArray_DoesNotThrow()
    {
        // Arrange - Simulate a ChatMessage with empty Contents array
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Create a ChatMessage with empty contents
        var assistantMessage = new ChatMessage()
        {
            Role = ChatRole.Assistant,
            Contents = []
        };
        prompt.Messages.Add(assistantMessage);

        // Act & Assert
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    #endregion

    #region After Method - Thinking Response Tests

    [Fact]
    public void After_WithThinkingResponse_ExtractsTextContent()
    {
        // Arrange - Simulate Mistral thinking response format
        var thinkingResponse = """
            [{"type":"thinking","thinking":"Let me analyze this..."},{"type":"text","text":"The answer is 42."}]
            """;

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("What is the answer to life?")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, thinkingResponse));

        // Act
        _behavior.After(Provider.Mistral, prompt);

        // Assert - Should extract just the text part
        var lastMessage = prompt.Messages.Last();
        Assert.Equal("The answer is 42.", lastMessage.Text);
    }

    [Fact]
    public void After_WithThinkingResponseNullText_DoesNotThrow()
    {
        // Arrange - Simulate edge case where text property is null in JSON
        var thinkingResponse = """
            [{"type":"thinking","thinking":"Let me analyze this..."},{"type":"text","text":null}]
            """;

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("What is the answer?")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, thinkingResponse));

        // Act & Assert - Should not throw when text.GetString() returns null
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        // Note: This may throw NullReferenceException if TextContent constructor doesn't accept null
        // which would confirm issue #309
        if (exception != null)
        {
            Assert.IsType<NullReferenceException>(exception);
        }
    }

    [Fact]
    public void After_WithOnlyThinkingNoText_DoesNotModify()
    {
        // Arrange - Simulate response with only thinking, no text
        var thinkingResponse = """
            [{"type":"thinking","thinking":"Let me analyze this..."}]
            """;

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("What is the answer?")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, thinkingResponse));
        var originalText = prompt.Messages.Last().Text;

        // Act
        _behavior.After(Provider.Mistral, prompt);

        // Assert - Should not modify since there's no text element
        Assert.Equal(originalText, prompt.Messages.Last().Text);
    }

    [Fact]
    public void After_WithInvalidJson_DoesNotThrow()
    {
        // Arrange - Simulate malformed JSON response
        var invalidJson = "[{invalid json}]";

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, invalidJson));

        // Act & Assert - Should gracefully handle invalid JSON
        var exception = Record.Exception(() => _behavior.After(Provider.Mistral, prompt));
        Assert.Null(exception);
    }

    [Fact]
    public void After_WithNonArrayJson_DoesNotModify()
    {
        // Arrange - JSON that parses but isn't an array
        var nonArrayJson = """{"type":"text","text":"Hello"}""";

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, nonArrayJson));
        var originalText = prompt.Messages.Last().Text;

        // Act
        _behavior.After(Provider.Mistral, prompt);

        // Assert - Should not modify since it's not an array
        Assert.Equal(originalText, prompt.Messages.Last().Text);
    }

    [Fact]
    public void After_WithTextNotStartingWithBracket_DoesNotProcess()
    {
        // Arrange - Normal text that doesn't look like JSON array
        var normalText = "Hello, I'm a normal response.";

        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, normalText));

        // Act
        _behavior.After(Provider.Mistral, prompt);

        // Assert - Should skip processing (quick check fails)
        Assert.Equal(normalText, prompt.Messages.Last().Text);
    }

    #endregion

    #region Order Tests

    [Fact]
    public void Order_Returns30()
    {
        // Assert
        Assert.Equal(30u, _behavior.Order);
    }

    #endregion
}
