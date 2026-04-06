using System.Diagnostics;
using Cellm.AddIn;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Tests.Unit.Helpers;
using Xunit;

namespace Cellm.Tests.Unit;

public class PipelineTests : IClassFixture<PipelineTestFixture>
{
    private readonly PipelineTestFixture _fixture;

    public PipelineTests(PipelineTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.MockChatClient.DefaultResponse = "Test response";
        _fixture.MockChatClient.CallCount = 0;
        _fixture.MockChatClient.ReceivedMessages.Clear();
        _fixture.MockChatClient.ReceivedOptions.Clear();
    }

    public static IEnumerable<object[]> Providers =>
    [
        [Provider.Anthropic, "claude-sonnet-4-20250514"],
        [Provider.Gemini, "gemini-2.5-flash"],
        [Provider.OpenAi, "gpt-4.1-mini"],
        [Provider.Mistral, "mistral-small-latest"],
        [Provider.DeepSeek, "deepseek-chat"],
        [Provider.Ollama, "llama3.2"],
        [Provider.OpenRouter, "openai/gpt-4.1-mini"],
    ];

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task GetResponseAsync_ReturnsResponseAsync(Provider provider, string model)
    {
        var arguments = new Arguments(
            provider,
            model,
            Array.Empty<Cellm.AddIn.Range>(),
            "Say hello",
            0.5,
            StructuredOutputShape.None);

        var result = await CellmFunctions.GetResponseAsync(
            arguments,
            Stopwatch.StartNew(),
            "A1",
            CancellationToken.None);

        Assert.Equal("Test response", result);
        Assert.True(_fixture.MockChatClient.CallCount > 0);
    }

    [Fact]
    public async Task GetResponseAsync_Gemini_ScalesTemperatureAsync()
    {
        var arguments = new Arguments(
            Provider.Gemini,
            "gemini-2.5-flash",
            Array.Empty<Cellm.AddIn.Range>(),
            "Say hello",
            0.5,
            StructuredOutputShape.None);

        await CellmFunctions.GetResponseAsync(
            arguments,
            Stopwatch.StartNew(),
            "A1",
            CancellationToken.None);

        // GeminiTemperatureBehavior scales 0-1 to 0-2, so 0.5 becomes 1.0
        var options = _fixture.MockChatClient.ReceivedOptions.Last();
        Assert.NotNull(options);
        Assert.Equal(1.0f, options.Temperature);
    }

    [Fact]
    public async Task GetResponseAsync_WithStructuredOutput_ParsesJsonArrayAsync()
    {
        _fixture.MockChatClient.DefaultResponse = """{"data": ["hello", "world"]}""";

        var arguments = new Arguments(
            Provider.OpenAi,
            "gpt-4.1-mini",
            Array.Empty<Cellm.AddIn.Range>(),
            "Return a list",
            0.5,
            StructuredOutputShape.Row);

        var result = await CellmFunctions.GetResponseAsync(
            arguments,
            Stopwatch.StartNew(),
            "A1",
            CancellationToken.None);

        // StructuredOutput.TryParse should parse the JSON into a string[,]
        Assert.IsType<string[,]>(result);
        var array = (string[,])result;
        Assert.Equal("hello", array[0, 0]);
        Assert.Equal("world", array[0, 1]);
    }

    [Fact]
    public async Task GetResponseAsync_CancellationRequested_ThrowsOrReturnsCancelledAsync()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var arguments = new Arguments(
            Provider.OpenAi,
            "gpt-4.1-mini",
            Array.Empty<Cellm.AddIn.Range>(),
            "Say hello",
            0.5,
            StructuredOutputShape.None);

        var result = await CellmFunctions.GetResponseAsync(
            arguments,
            Stopwatch.StartNew(),
            "A1",
            cts.Token);

        // GetResponseAsync catches OperationCanceledException and returns a string
        Assert.IsType<string>(result);
        Assert.Contains("cancelled", result.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

}
