using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Polly.RateLimiting;
using Polly.Registry;
using Polly.Timeout;
using Xunit;

namespace Cellm.Tests.Unit.Models;

public class ClientTests
{
    private readonly ISender _sender;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly Client _client;

    public ClientTests()
    {
        _sender = Substitute.For<ISender>();

        // Create a simple pass-through pipeline for testing
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder<Prompt>("RateLimiter", (builder, _) =>
        {
            // Empty pipeline - just passes through
        });
        _pipelineProvider = registry;

        _client = new Client(_sender, _pipelineProvider);
    }

    private static Prompt CreateTestPrompt(string modelId = "test-model")
    {
        return new PromptBuilder()
            .SetModel(modelId)
            .AddUserMessage("Test message")
            .Build();
    }

    [Fact]
    public async Task GetResponseAsync_SuccessfulRequest_ReturnsPrompt()
    {
        // Arrange
        var request = CreateTestPrompt();
        var expectedResponse = new PromptBuilder()
            .SetModel("test-model")
            .AddAssistantMessage("Test response")
            .Build();

        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderResponse(expectedResponse, null!));

        // Act
        var result = await _client.GetResponseAsync(request, Provider.Ollama, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetResponseAsync_RateLimitExceeded_ThrowsCellmException()
    {
        // Arrange
        var request = CreateTestPrompt("gpt-4");
        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RateLimiterRejectedException());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CellmException>(
            () => _client.GetResponseAsync(request, Provider.OpenAi, CancellationToken.None));

        Assert.Contains("rate limit exceeded", ex.Message);
        Assert.Contains("OpenAi", ex.Message);
        Assert.Contains("gpt-4", ex.Message);
        Assert.IsType<RateLimiterRejectedException>(ex.InnerException);
    }

    [Fact]
    public async Task GetResponseAsync_Timeout_ThrowsCellmException()
    {
        // Arrange
        var request = CreateTestPrompt("claude-3");
        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutRejectedException());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CellmException>(
            () => _client.GetResponseAsync(request, Provider.Anthropic, CancellationToken.None));

        Assert.Contains("timed out", ex.Message);
        Assert.Contains("Anthropic", ex.Message);
        Assert.Contains("claude-3", ex.Message);
        Assert.IsType<TimeoutRejectedException>(ex.InnerException);
    }

    [Fact]
    public async Task GetResponseAsync_RateLimitMessage_IncludesProviderAndModel()
    {
        // Arrange
        var modelId = "llama3.2";
        var provider = Provider.Ollama;
        var request = CreateTestPrompt(modelId);

        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RateLimiterRejectedException());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CellmException>(
            () => _client.GetResponseAsync(request, provider, CancellationToken.None));

        // Verify the message format: "{provider}/{modelId} rate limit exceeded"
        Assert.Equal($"{provider}/{modelId} rate limit exceeded", ex.Message);
    }

    [Fact]
    public async Task GetResponseAsync_TimeoutMessage_IncludesProviderAndModel()
    {
        // Arrange
        var modelId = "gemini-pro";
        var provider = Provider.Gemini;
        var request = CreateTestPrompt(modelId);

        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutRejectedException());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CellmException>(
            () => _client.GetResponseAsync(request, provider, CancellationToken.None));

        // Verify the message format: "{provider}/{modelId} request timed out"
        Assert.Equal($"{provider}/{modelId} request timed out", ex.Message);
    }

    [Fact]
    public async Task GetResponseAsync_OtherException_PropagatesUnchanged()
    {
        // Arrange
        var request = CreateTestPrompt();
        var originalException = new InvalidOperationException("Some other error");

        _sender.Send(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(originalException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.GetResponseAsync(request, Provider.Ollama, CancellationToken.None));

        Assert.Same(originalException, ex);
    }
}
