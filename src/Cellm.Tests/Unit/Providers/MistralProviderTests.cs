using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Microsoft.Extensions.AI;
using NSubstitute;
using Xunit;

namespace Cellm.Tests.Unit.Providers;

/// <summary>
/// Tests to reproduce issue #309: "Mistral provider broken"
/// Error: "Object reference not set to an instance of an object"
///
/// The issue occurs when the Mistral SDK's IChatClient adapter returns a ChatMessage
/// where the Text property is null or the Contents collection doesn't properly populate
/// the Text property.
/// </summary>
public class MistralProviderTests
{
    /// <summary>
    /// Issue #309 Reproduction: Tests that a ChatMessage with null Text property
    /// can be safely accessed without throwing NullReferenceException.
    ///
    /// The Mistral SDK 2.3.0 may return ChatMessages where:
    /// - Text property is null
    /// - Contents array is empty
    /// - Role is set but content is missing
    /// </summary>
    [Fact]
    public void ChatMessage_WithNullText_SafeAccess()
    {
        // Arrange - Simulate Mistral SDK response with null text
        var chatMessage = new ChatMessage(ChatRole.Assistant, (string?)null);

        // Act & Assert - These accesses should not throw
        Assert.Equal(ChatRole.Assistant, chatMessage.Role);
        Assert.Null(chatMessage.Text);  // Text should be null, not throw
        Assert.NotNull(chatMessage.Contents);  // Contents should be initialized
    }

    [Fact]
    public void ChatMessage_WithEmptyContents_TextIsNull()
    {
        // Arrange - Simulate Mistral SDK response with empty contents
        var chatMessage = new ChatMessage
        {
            Role = ChatRole.Assistant,
            Contents = []
        };

        // Act & Assert
        Assert.Null(chatMessage.Text);  // Text derived from empty Contents should be null
    }

    [Fact]
    public void ChatResponse_WithNullTextMessage_LastOrDefaultTextAccess()
    {
        // Arrange - Simulate what CellmFunctions.GetResponseAsync does at line 317
        // var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, (string?)null)  // Simulating Mistral SDK returning null text
        };

        var chatResponse = new ChatResponse(messages);

        // Act - This is what CellmFunctions does
        var assistantMessage = chatResponse.Messages.LastOrDefault()?.Text;

        // Assert - This should not throw, but should return null
        Assert.Null(assistantMessage);
    }

    /// <summary>
    /// This test demonstrates the exact flow that causes issue #309.
    /// When Mistral SDK returns a ChatMessage with null/empty content,
    /// the following code in CellmFunctions.cs:317 will throw InvalidOperationException:
    ///
    /// var assistantMessage = response.Messages.LastOrDefault()?.Text
    ///     ?? throw new InvalidOperationException("No text response");
    ///
    /// However, the "Object reference not set to an instance of an object" error
    /// suggests that somewhere in the pipeline, null is being dereferenced.
    /// This could be in:
    /// 1. MistralThinkingBehavior.After() - accessing .Text.Trim() on null Text
    /// 2. Other behaviors that process the response
    /// </summary>
    [Fact]
    public void Prompt_WithNullTextAssistantMessage_AddedMessages()
    {
        // Arrange
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Simulate adding a response message with null text (from Mistral SDK)
        var responseMessage = new ChatMessage(ChatRole.Assistant, (string?)null);

        // Act - Simulate what ProviderRequestHandler does
        var newPrompt = new PromptBuilder(prompt)
            .AddMessage(responseMessage)
            .Build();

        // Assert
        Assert.Equal(2, newPrompt.Messages.Count);
        Assert.Null(newPrompt.Messages.Last().Text);
    }

    /// <summary>
    /// Tests the scenario where Mistral SDK returns a ChatResponse with messages
    /// but the ChatResponse constructor may have issues with null content.
    /// </summary>
    [Fact]
    public void ChatResponse_Construction_WithNullTextMessages()
    {
        // Arrange
        var assistantMessage = new ChatMessage(ChatRole.Assistant, (string?)null);

        // Act - Create ChatResponse like the SDK would
        var chatResponse = new ChatResponse(assistantMessage);

        // Assert
        Assert.Single(chatResponse.Messages);
        Assert.Null(chatResponse.Messages.First().Text);
    }

    /// <summary>
    /// Mock test to verify IChatClient returns proper response.
    /// This tests what happens when GetResponseAsync returns a message with null text.
    /// </summary>
    [Fact]
    public async Task MockedChatClient_ReturnsNullTextResponse_HandledGracefullyAsync()
    {
        // Arrange
        var mockChatClient = Substitute.For<IChatClient>();
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, (string?)null));

        mockChatClient.GetResponseAsync(
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "mistral-small-latest" };

        // Act
        var response = await mockChatClient.GetResponseAsync(messages, options, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Single(response.Messages);
        var assistantMessage = response.Messages.Last();
        Assert.Equal(ChatRole.Assistant, assistantMessage.Role);
        Assert.Null(assistantMessage.Text);  // Key assertion - text should be null without throwing
    }

    /// <summary>
    /// Test the exact pattern used in CellmFunctions.cs:317
    /// This should throw InvalidOperationException, not NullReferenceException.
    /// If NullReferenceException is thrown, it indicates an issue in the SDK or behaviors.
    /// </summary>
    [Fact]
    public void CellmFunctionsPattern_WithNullText_ThrowsInvalidOperationException()
    {
        // Arrange - Simulate the exact scenario from CellmFunctions.cs
        var prompt = new PromptBuilder()
            .SetModel("mistral-small-latest")
            .AddUserMessage("Hello")
            .Build();

        // Simulate Mistral response with null text
        prompt.Messages.Add(new ChatMessage(ChatRole.Assistant, (string?)null));

        // Act & Assert - Using the exact pattern from CellmFunctions.cs:317
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var assistantMessage = prompt.Messages.LastOrDefault()?.Text
                ?? throw new InvalidOperationException("No text response");
        });

        Assert.Equal("No text response", exception.Message);
    }
}
