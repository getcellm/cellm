using Microsoft.Extensions.AI;
using System.Text.Json;
using Xunit;

namespace Cellm.Tests.Unit.Providers;

/// <summary>
/// Tests to reproduce the exact bug in Mistral.SDK v2.3.0 that causes issue #309.
///
/// ROOT CAUSE ANALYSIS:
/// ====================
/// The bug is in Mistral.SDK's CompletionsEndpoint.ChatClient.cs in the ProcessResponseContent method:
///
/// ```csharp
/// private static List&lt;AIContent&gt; ProcessResponseContent(ChatCompletionResponse response)
/// {
///     List&lt;AIContent&gt; contents = new();
///     foreach (var content in response.Choices)
///     {
///         if (content.Message.ToolCalls is not null)  // &lt;-- BUG: No null check on content.Message!
///         {
///             contents.Add(new TextContent(content.Message.Content));
///             // ...
///         }
///         else
///         {
///             contents.Add(new TextContent(content.Message.Content));
///         }
///     }
///     return contents;
/// }
/// ```
///
/// When the Mistral API returns a response where Choice.Message is null (which can happen
/// in certain edge cases), accessing content.Message.ToolCalls throws NullReferenceException.
///
/// The streaming code in GetStreamingResponseAsync correctly uses choice.Delta?.ToolCalls
/// with null-conditional operator, but ProcessResponseContent does NOT.
///
/// COMPARISON:
/// - Streaming (CORRECT): choice.Delta?.ToolCalls
/// - Non-streaming (BUG): content.Message.ToolCalls
/// </summary>
public class MistralSdkBugReproductionTests
{
    /// <summary>
    /// Simulates the exact bug in ProcessResponseContent when Choice.Message is null.
    /// This demonstrates the NullReferenceException that causes issue #309.
    /// </summary>
    [Fact]
    public void ProcessResponseContent_WhenMessageIsNull_ThrowsNullReferenceException()
    {
        // Arrange - Simulate Mistral API JSON response with null message
        // This can happen in certain edge cases like incomplete responses or API errors
        var jsonResponse = """
        {
            "id": "test-id",
            "object": "chat.completion",
            "created": 1234567890,
            "model": "mistral-small-latest",
            "choices": [
                {
                    "index": 0,
                    "finish_reason": "stop"
                }
            ]
        }
        """;
        // Note: "message" field is missing, which deserializes to null

        var response = JsonSerializer.Deserialize<SimulatedChatCompletionResponse>(jsonResponse);

        // Act & Assert - This simulates what ProcessResponseContent does
        var exception = Record.Exception(() =>
        {
            var contents = new List<AIContent>();
            foreach (var choice in response!.Choices!)
            {
                // This is the exact bug: no null check before accessing Message.ToolCalls
                if (choice.Message.ToolCalls is not null)  // CRASH HERE!
                {
                    contents.Add(new TextContent(choice.Message.Content));
                }
                else
                {
                    contents.Add(new TextContent(choice.Message.Content));
                }
            }
        });

        // The bug causes NullReferenceException
        Assert.NotNull(exception);
        Assert.IsType<NullReferenceException>(exception);
    }

    /// <summary>
    /// Demonstrates the correct fix: using null-conditional operator like the streaming code does.
    /// </summary>
    [Fact]
    public void ProcessResponseContent_WithNullCheck_DoesNotThrow()
    {
        // Arrange - Same response with null message
        var jsonResponse = """
        {
            "id": "test-id",
            "object": "chat.completion",
            "created": 1234567890,
            "model": "mistral-small-latest",
            "choices": [
                {
                    "index": 0,
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<SimulatedChatCompletionResponse>(jsonResponse);

        // Act - Using the CORRECT pattern with null-conditional operator
        var exception = Record.Exception(() =>
        {
            var contents = new List<AIContent>();
            foreach (var choice in response!.Choices!)
            {
                // FIXED: Using null-conditional operator like streaming code does
                if (choice.Message?.ToolCalls is not null)  // Safe!
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
                else
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
            }
        });

        // No exception when using proper null checking
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests with a response that has a message but null content (another edge case).
    /// This is valid according to Mistral API when assistant returns tool_calls instead of content.
    /// </summary>
    [Fact]
    public void ProcessResponseContent_WhenContentIsNull_HandledGracefully()
    {
        // Arrange - Message exists but content is null (common with tool calls)
        var jsonResponse = """
        {
            "id": "test-id",
            "object": "chat.completion",
            "created": 1234567890,
            "model": "mistral-small-latest",
            "choices": [
                {
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": null,
                        "tool_calls": null
                    },
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<SimulatedChatCompletionResponse>(jsonResponse);

        // Act - TextContent accepts null
        var exception = Record.Exception(() =>
        {
            var contents = new List<AIContent>();
            foreach (var choice in response!.Choices!)
            {
                if (choice.Message?.ToolCalls is not null)
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
                else
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
            }
        });

        // Should not throw - TextContent accepts null
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests with an empty choices array - another edge case.
    /// </summary>
    [Fact]
    public void ProcessResponseContent_WhenChoicesEmpty_HandledGracefully()
    {
        var jsonResponse = """
        {
            "id": "test-id",
            "object": "chat.completion",
            "created": 1234567890,
            "model": "mistral-small-latest",
            "choices": []
        }
        """;

        var response = JsonSerializer.Deserialize<SimulatedChatCompletionResponse>(jsonResponse);

        var exception = Record.Exception(() =>
        {
            var contents = new List<AIContent>();
            foreach (var choice in response!.Choices!)
            {
                if (choice.Message?.ToolCalls is not null)
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
                else
                {
                    contents.Add(new TextContent(choice.Message?.Content));
                }
            }
        });

        Assert.Null(exception);
    }

    #region Simulated Mistral SDK DTOs

    /// <summary>
    /// Simulates the Mistral.SDK.DTOs.ChatCompletionResponse class
    /// </summary>
    private class SimulatedChatCompletionResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public int Created { get; set; }
        public string? Model { get; set; }
        public List<SimulatedChoice>? Choices { get; set; }
    }

    /// <summary>
    /// Simulates the Mistral.SDK.DTOs.Choice class
    /// </summary>
    private class SimulatedChoice
    {
        public int Index { get; set; }
        public SimulatedChatMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Simulates the Mistral.SDK.DTOs.ChatMessage class
    /// </summary>
    private class SimulatedChatMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public List<object>? ToolCalls { get; set; }
    }

    #endregion
}
