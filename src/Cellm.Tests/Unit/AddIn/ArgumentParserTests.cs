using Cellm.AddIn;
using Xunit;
using CellmRange = Cellm.AddIn.Range;

namespace Cellm.Tests.Unit.AddIn;

public class ArgumentParserTests
{
    #region GetColumnName Tests

    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void GetColumnName_ReturnsCorrectColumnLetter(int columnIndex, string expected)
    {
        var result = ArgumentParser.GetColumnName(columnIndex);

        Assert.Equal(expected, result);
    }

    #endregion

    #region GetRowName Tests

    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "2")]
    [InlineData(9, "10")]
    [InlineData(99, "100")]
    [InlineData(999, "1000")]
    public void GetRowName_ReturnsCorrectRowNumber(int rowIndex, string expected)
    {
        var result = ArgumentParser.GetRowName(rowIndex);

        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatRanges Tests

    [Fact]
    public void FormatRanges_WrapsInCellsTags()
    {
        var ranges = "some range content";

        var result = ArgumentParser.FormatRanges(ranges);

        Assert.Contains(ArgumentParser.CellsBeginTag, result);
        Assert.Contains(ArgumentParser.CellsEndTag, result);
        Assert.Contains(ranges, result);
    }

    [Fact]
    public void FormatRanges_EmptyString_StillWrapsInTags()
    {
        var result = ArgumentParser.FormatRanges(string.Empty);

        Assert.Contains(ArgumentParser.CellsBeginTag, result);
        Assert.Contains(ArgumentParser.CellsEndTag, result);
    }

    #endregion

    #region FormatInstructions Tests

    [Fact]
    public void FormatInstructions_WrapsInInstructionsTags()
    {
        var instructions = "Do something interesting";

        var result = ArgumentParser.FormatInstructions(instructions);

        Assert.Contains(ArgumentParser.InstructionsBeginTag, result);
        Assert.Contains(ArgumentParser.InstructionsEndTag, result);
        Assert.Contains(instructions, result);
    }

    #endregion

    #region RenderRange Tests

    [Fact]
    public void RenderRange_SingleCell_ReturnsMarkdownTable()
    {
        var range = new CellmRange(0, 0, "Hello");

        var result = ArgumentParser.RenderRange(range);

        Assert.Contains("| Row \\ Col |", result);
        Assert.Contains("| A", result);  // Column A header (may have padding)
        Assert.Contains("| 1", result);   // Row 1
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void RenderRange_MultipleRows_ReturnsMarkdownTable()
    {
        var values = new object[2, 2]
        {
            { "A1", "B1" },
            { "A2", "B2" }
        };
        var range = new CellmRange(0, 0, values);

        var result = ArgumentParser.RenderRange(range);

        Assert.Contains("| Row \\ Col |", result);
        Assert.Contains("| A", result);  // Column headers (may have padding)
        Assert.Contains("| B", result);
        Assert.Contains("A1", result);
        Assert.Contains("B1", result);
        Assert.Contains("A2", result);
        Assert.Contains("B2", result);
    }

    [Fact]
    public void RenderRange_EmptyRange_ReturnsEmptyMessage()
    {
        var values = new object[2, 2]
        {
            { string.Empty, string.Empty },
            { string.Empty, string.Empty }
        };
        var range = new CellmRange(0, 0, values);

        var result = ArgumentParser.RenderRange(range);

        Assert.Contains("all cells are empty", result);
    }

    [Fact]
    public void RenderRange_WithSpecialCharacters_EscapesPipeCharacter()
    {
        var range = new CellmRange(0, 0, "Value|WithPipe");

        var result = ArgumentParser.RenderRange(range);

        Assert.Contains("Value\\|WithPipe", result);
    }

    [Fact]
    public void RenderRange_SparseData_SkipsEmptyRowsAndColumns()
    {
        var values = new object[3, 3]
        {
            { "A1", string.Empty, string.Empty },
            { string.Empty, string.Empty, string.Empty },
            { "A3", string.Empty, "C3" }
        };
        var range = new CellmRange(0, 0, values);

        var result = ArgumentParser.RenderRange(range);

        Assert.Contains("A1", result);
        Assert.Contains("A3", result);
        Assert.Contains("C3", result);
        // Should only include columns A and C (with values, may have padding)
        Assert.Contains("| A", result);
        Assert.Contains("| C", result);
        // Verify column B is not included (check that it doesn't appear in header row)
        Assert.DoesNotContain("| B", result);
    }

    #endregion

    #region RenderRanges Tests

    [Fact]
    public void RenderRanges_NoRanges_ReturnsNoContextMessage()
    {
        var ranges = new List<CellmRange>();

        var result = ArgumentParser.RenderRanges(ranges);

        Assert.Contains("no additional context", result);
    }

    [Fact]
    public void RenderRanges_MultipleRanges_JoinsWithNewline()
    {
        var ranges = new List<CellmRange>
        {
            new(0, 0, "First"),
            new(0, 1, "Second")
        };

        var result = ArgumentParser.RenderRanges(ranges);

        Assert.Contains("First", result);
        Assert.Contains("Second", result);
    }

    #endregion
}
