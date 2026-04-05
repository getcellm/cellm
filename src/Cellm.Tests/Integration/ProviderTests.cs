using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Cellm.Tests.Integration;

[Trait("Category", "Integration")]
public class ProviderTests : IClassFixture<ProviderTestFixture>, IDisposable
{
    private readonly ProviderTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ProviderTests(ProviderTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public void Dispose() { }

    public static IEnumerable<object[]> Providers =>
    [
        [Provider.Anthropic],
        [Provider.DeepSeek],
        [Provider.Gemini],
        [Provider.Mistral],
        [Provider.Ollama],
        [Provider.OpenAi],
        [Provider.OpenRouter],
    ];

    private void SkipIfUnavailable(Provider provider)
    {
        if (!_fixture.IsProviderAvailable(provider))
        {
            Assert.Fail($"{provider}: no API key configured");
        }
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task BasicPrompt_ReturnsResponseAsync(Provider provider)
    {
        SkipIfUnavailable(provider);

        var model = _fixture.GetDefaultModel(provider);
        var prompt = new PromptBuilder()
            .SetModel(model)
            .SetTemperature(0)
            .AddUserMessage("What is 2+2? Reply with just the number.")
            .Build();

        var response = await _fixture.Client.GetResponseAsync(prompt, provider, CancellationToken.None);

        var text = response.Messages.Last().Text ?? string.Empty;
        _output.WriteLine($"{provider}/{model}: {text}");
        Assert.Contains("4", text);
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task ToolUse_FileReader_ReturnsFileContentAsync(Provider provider)
    {
        SkipIfUnavailable(provider);

        var tempDir = Path.Combine(Path.GetTempPath(), $"cellm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "secret.txt");
        try
        {
            var content = "The secret answer is: pineapple42";
            await File.WriteAllTextAsync(tempFile, content);

            var model = _fixture.GetDefaultModel(provider);
            var prompt = new PromptBuilder()
                .SetModel(model)
                .SetTemperature(0)
                .AddSystemMessage("You have access to a FileReaderRequest tool. Use it to read files when asked.")
                .AddUserMessage($"Use the FileReaderRequest tool to read the file at the following path and tell me the secret answer: {tempFile}")
                .Build();

            var response = await _fixture.Client.GetResponseAsync(prompt, provider, CancellationToken.None);

            var text = response.Messages.Last().Text ?? string.Empty;
            _output.WriteLine($"{provider}/{model}: {text}");
            Assert.Contains("pineapple42", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task ToolUse_FileSearch_FindsFileAsync(Provider provider)
    {
        SkipIfUnavailable(provider);

        var tempDir = Path.Combine(Path.GetTempPath(), $"cellm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var fileName = "unicorn_report_2024.txt";
            var filePath = Path.Combine(tempDir, fileName);
            await File.WriteAllTextAsync(filePath, "test content");

            var model = _fixture.GetDefaultModel(provider);
            var prompt = new PromptBuilder()
                .SetModel(model)
                .SetTemperature(0)
                .AddUserMessage($"Search for files matching *.txt in {tempDir}. What is the name of the file you found? Reply with just the filename.")
                .Build();

            var response = await _fixture.Client.GetResponseAsync(prompt, provider, CancellationToken.None);

            var text = response.Messages.Last().Text ?? string.Empty;
            _output.WriteLine($"{provider}/{model}: {text}");
            Assert.Contains("unicorn_report_2024", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public async Task McpToolUse_Playwright_NavigatesPageAsync(Provider provider)
    {
        SkipIfUnavailable(provider);

        var model = _fixture.GetDefaultModel(provider);
        var prompt = new PromptBuilder()
            .SetModel(model)
            .SetTemperature(0)
            .AddUserMessage("Use the browser tool to navigate to https://example.com and tell me the page title. Reply with just the title.")
            .Build();

        var response = await _fixture.Client.GetResponseAsync(prompt, provider, CancellationToken.None);

        var text = response.Messages.Last().Text ?? string.Empty;
        _output.WriteLine($"{provider}/{model}: {text}");
        Assert.Contains("Example Domain", text, StringComparison.OrdinalIgnoreCase);
    }
}
