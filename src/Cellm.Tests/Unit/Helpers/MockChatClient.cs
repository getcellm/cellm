using Microsoft.Extensions.AI;

namespace Cellm.Tests.Unit.Helpers;

/// <summary>
/// A simple deterministic IChatClient mock for unit testing without API calls.
/// </summary>
public class MockChatClient : IChatClient
{
    public string DefaultResponse { get; set; } = "Test response";
    public int CallCount { get; private set; }
    public List<IList<ChatMessage>> ReceivedMessages { get; } = [];

    public ChatClientMetadata Metadata => new("MockChatClient", null, "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        ReceivedMessages.Add(chatMessages.ToList());

        var responseMessage = new ChatMessage(ChatRole.Assistant, DefaultResponse);
        var response = new ChatResponse(responseMessage)
        {
            ModelId = options?.ModelId ?? "mock-model"
        };

        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not supported in mock");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
    }
}
