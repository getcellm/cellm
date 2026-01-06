using Cellm.AddIn;
using Cellm.Models.Behaviors;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Cellm.Tests.Unit.Behaviors;

public class CacheBehaviorTests
{
    private readonly HybridCache _cache;
    private readonly IOptionsMonitor<CellmAddInConfiguration> _configMonitor;
    private readonly ILogger<CacheBehavior<ProviderRequest, ProviderResponse>> _logger;

    public CacheBehaviorTests()
    {
        // Set up real HybridCache from DI
        var services = new ServiceCollection();
        services.AddLogging();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var serviceProvider = services.BuildServiceProvider();

        _cache = serviceProvider.GetRequiredService<HybridCache>();
        _logger = serviceProvider.GetRequiredService<ILogger<CacheBehavior<ProviderRequest, ProviderResponse>>>();

        // Mock configuration
        _configMonitor = Substitute.For<IOptionsMonitor<CellmAddInConfiguration>>();
    }

    private CacheBehavior<ProviderRequest, ProviderResponse> CreateBehavior()
    {
        return new CacheBehavior<ProviderRequest, ProviderResponse>(_cache, _configMonitor, _logger);
    }

    private static ProviderRequest CreateRequest(string message = "Test message")
    {
        var prompt = new PromptBuilder()
            .SetModel("test-model")
            .AddUserMessage(message)
            .Build();

        return new ProviderRequest(prompt, Provider.Ollama);
    }

    private static ProviderResponse CreateResponse(string message = "Test response")
    {
        var prompt = new PromptBuilder()
            .SetModel("test-model")
            .AddAssistantMessage(message)
            .Build();

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, message));
        return new ProviderResponse(prompt, chatResponse);
    }

    #region Cache Disabled Tests

    [Fact]
    public async Task Handle_WhenCacheDisabled_CallsNextDirectly()
    {
        // Arrange
        _configMonitor.CurrentValue.Returns(new CellmAddInConfiguration { EnableCache = false });
        var behavior = CreateBehavior();
        var request = CreateRequest();
        var expectedResponse = CreateResponse();
        var nextCallCount = 0;

        // Act
        var result1 = await behavior.Handle(request, async ct =>
        {
            nextCallCount++;
            return expectedResponse;
        }, CancellationToken.None);

        var result2 = await behavior.Handle(request, async ct =>
        {
            nextCallCount++;
            return expectedResponse;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(2, nextCallCount); // Called twice, no caching
        Assert.Equal(expectedResponse, result1);
        Assert.Equal(expectedResponse, result2);
    }

    #endregion

    #region Cache Enabled Tests

    [Fact]
    public async Task Handle_WhenCacheEnabled_ReturnsCachedResponse()
    {
        // Arrange
        _configMonitor.CurrentValue.Returns(new CellmAddInConfiguration
        {
            EnableCache = true,
            CacheTimeoutInSeconds = 60
        });
        var behavior = CreateBehavior();
        var request = CreateRequest("Unique message for caching test");
        var expectedResponse = CreateResponse("Cached response");
        var nextCallCount = 0;

        // Act
        var result1 = await behavior.Handle(request, async ct =>
        {
            nextCallCount++;
            return expectedResponse;
        }, CancellationToken.None);

        var result2 = await behavior.Handle(request, async ct =>
        {
            nextCallCount++;
            return expectedResponse;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(1, nextCallCount); // Called only once, second call uses cache
        // Note: HybridCache serializes/deserializes, so we compare properties not references
        Assert.Equal(expectedResponse.Prompt.Options.ModelId, result1.Prompt.Options.ModelId);
        Assert.Equal(expectedResponse.Prompt.Options.ModelId, result2.Prompt.Options.ModelId);
    }

    [Fact]
    public async Task Handle_DifferentPrompts_CacheMiss()
    {
        // Arrange
        _configMonitor.CurrentValue.Returns(new CellmAddInConfiguration
        {
            EnableCache = true,
            CacheTimeoutInSeconds = 60
        });
        var behavior = CreateBehavior();
        var request1 = CreateRequest("First message");
        var request2 = CreateRequest("Second message");
        var nextCallCount = 0;

        // Act
        await behavior.Handle(request1, async ct =>
        {
            nextCallCount++;
            return CreateResponse($"Response {nextCallCount}");
        }, CancellationToken.None);

        await behavior.Handle(request2, async ct =>
        {
            nextCallCount++;
            return CreateResponse($"Response {nextCallCount}");
        }, CancellationToken.None);

        // Assert
        Assert.Equal(2, nextCallCount); // Called twice for different prompts
    }

    #endregion

    #region Cache Key with Tools Tests

    [Fact]
    public async Task Handle_SamePromptDifferentTools_CacheMiss()
    {
        // Arrange
        _configMonitor.CurrentValue.Returns(new CellmAddInConfiguration
        {
            EnableCache = true,
            CacheTimeoutInSeconds = 60
        });
        var behavior = CreateBehavior();

        // Create two prompts with same message but different tools
        var prompt1 = new PromptBuilder()
            .SetModel("test-model")
            .AddUserMessage("Same message")
            .SetTools(new List<AITool>())
            .Build();

        var prompt2 = new PromptBuilder()
            .SetModel("test-model")
            .AddUserMessage("Same message")
            .SetTools(new List<AITool> { AIFunctionFactory.Create(() => "test", "TestTool") })
            .Build();

        var request1 = new ProviderRequest(prompt1, Provider.Ollama);
        var request2 = new ProviderRequest(prompt2, Provider.Ollama);

        var nextCallCount = 0;

        // Act
        await behavior.Handle(request1, async ct =>
        {
            nextCallCount++;
            return CreateResponse($"Response {nextCallCount}");
        }, CancellationToken.None);

        await behavior.Handle(request2, async ct =>
        {
            nextCallCount++;
            return CreateResponse($"Response {nextCallCount}");
        }, CancellationToken.None);

        // Assert
        Assert.Equal(2, nextCallCount); // Different tools = cache miss
    }

    #endregion
}
