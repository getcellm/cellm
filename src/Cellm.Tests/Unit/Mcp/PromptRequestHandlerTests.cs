using Cellm.AddIn.Control;
using Xunit;

namespace Cellm.Tests.Unit.Mcp;

public class PromptRequestHandlerTests
{
    [Fact]
    public void BuildFormula_UsesDefaultProvider()
    {
        var formula = PromptRequestHandler.BuildFormula("Summarize this", ["A1:B2"], null);

        Assert.Equal("=PROMPT(\"Summarize this\", A1:B2)", formula);
    }

    [Fact]
    public void BuildFormula_UsesExplicitProviderAndEscapesQuotes()
    {
        var formula = PromptRequestHandler.BuildFormula("Say \"hello\"", [], "OpenAi/gpt-5");

        Assert.Equal("=PROMPTMODEL(\"OpenAi/gpt-5\", \"Say \"\"hello\"\"\")", formula);
    }
}
