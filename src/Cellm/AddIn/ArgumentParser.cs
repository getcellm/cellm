using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;

namespace Cellm.AddIn;

public class ArgumentParser(IConfiguration configuration)
{
    private object? _provider;
    private object? _model;
    private object? _instructionsOrCells;
    private object? _instructionsOrTemperature;
    private object? _temperature;
    private StructuredOutputShape _outputShape = StructuredOutputShape.None;

    public static readonly string CellsBeginTag = "<cells>";
    public static readonly string CellsEndTag = "</cells>";
    public static readonly string InstructionsBeginTag = "<user's instructions>";
    public static readonly string InstructionsEndTag = "<user's instructions>";

    public ArgumentParser AddProvider(object providerAndModel)
    {
        _provider = providerAndModel;

        return this;
    }

    public ArgumentParser AddModel(object providerAndModel)
    {
        _model = providerAndModel;

        return this;
    }

    public ArgumentParser AddInstructionsOrCells(object instructionsOrCells)
    {
        _instructionsOrCells = instructionsOrCells;

        return this;
    }

    public ArgumentParser AddInstructionsOrTemperature(object instructionsOrTemperature)
    {
        _instructionsOrTemperature = instructionsOrTemperature;

        return this;
    }

    public ArgumentParser AddTemperature(object temperature)
    {
        _temperature = temperature;

        return this;
    }

    internal ArgumentParser AddOutputShape(StructuredOutputShape outputShape)
    {
        _outputShape = outputShape;

        return this;
    }

    internal Arguments Parse()
    {
        var providerAsString = ParseProvider(_provider)
            ?? configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        if (!Enum.TryParse<Provider>(providerAsString, true, out var provider))
        {
            throw new ArgumentException($"Unsupported provider: {providerAsString}");
        }

        var model = ParseModel(_model)
            ?? configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        var defaultTemperature = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultTemperature)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultTemperature));

        var arguments = (_instructionsOrCells, _instructionsOrTemperature, _temperature) switch
        {
            // "=PROMPT("Hello world")
            (string instructions, ExcelMissing, ExcelMissing) => new Arguments(provider, model, string.Empty, instructions, ParseTemperature(Convert.ToDouble(defaultTemperature)), _outputShape),
            // "=PROMPT("Hello world", 0.7)
            (string instructions, double temperature, ExcelMissing) => new Arguments(provider, model, string.Empty, instructions, ParseTemperature(temperature), _outputShape),
            // "=PROMPT(A1:B2)
            (ExcelReference cells, ExcelMissing, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), SystemMessages.InlineInstructions, ParseTemperature(Convert.ToDouble(defaultTemperature)), _outputShape),
            // "=PROMPT(A1:B2, 0.7)
            (ExcelReference cells, double temperature, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), SystemMessages.InlineInstructions, ParseTemperature(Convert.ToDouble(defaultTemperature)), _outputShape),
            // "=PROMPT(A1:B2, "Extract keywords")
            (ExcelReference cells, string instructions, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), instructions, ParseTemperature(Convert.ToDouble(defaultTemperature)), _outputShape),
            // "=PROMPT(A1:B2, "Extract keywords", 0.7)
            (ExcelReference cells, string instructions, double temperature) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), instructions, ParseTemperature(temperature), _outputShape),
            // "=PROMPT(A1:B2, C1:D2)
            (ExcelReference cells, ExcelReference instructions, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), new Cells(instructions.RowFirst, instructions.RowLast, instructions.GetValue()), ParseTemperature(Convert.ToDouble(defaultTemperature)), _outputShape),
            // "=PROMPT(A1:B2, C1:D2, 0.7)
            (ExcelReference cells, ExcelReference instructions, double temperature) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, cells.GetValue()), new Cells(instructions.RowFirst, instructions.RowLast, instructions.GetValue()), ParseTemperature(temperature), _outputShape),
            // Anything else
            _ => throw new ArgumentException($"Invalid arguments ({_instructionsOrCells?.GetType().Name}, {_instructionsOrTemperature?.GetType().Name}, {_temperature?.GetType().Name})")
        };

        if (arguments.Cells is Cells contextCells && contextCells.Values is ExcelError contextCellsError)
        {
            throw new ExcelErrorException(contextCellsError);
        }

        if (arguments.Instructions is Cells instructionCells && instructionCells.Values is ExcelError instructionsCellsError)
        {
            throw new ExcelErrorException(instructionsCellsError);
        }

        return arguments;
    }

    internal static string AddCells(string cells)
    {
        return new StringBuilder()
            .AppendLine(CellsBeginTag)
            .AppendLine(cells)
            .AppendLine(CellsEndTag)
            .ToString();
    }

    internal static string AddInstructions(string instructions)
    {
        return new StringBuilder()
            .AppendLine(InstructionsBeginTag)
            .AppendLine(instructions)
            .AppendLine(InstructionsEndTag)
            .ToString();
    }

    // Render sheet as Markdown table because models have seen loads of those
    internal static string ParseCells(Cells cells)
    {
        var values = cells.Values switch
        {
            ExcelError excelError => throw new ExcelErrorException(excelError),
            object[,] manyCells => manyCells,
            object singleCell => new object[1, 1] { { singleCell } }
        };

        var numberOfRows = values.GetLength(0);
        var numberOfColumns = values.GetLength(1);

        var numberOfRenderedRows = numberOfRows + 1; // Includes the header row
        var numberOfRenderedColumns = numberOfColumns + 1; // Includes the row enumeration column

        // We go over the table twice, once to parse values and once to build the final string
        var table = new string[numberOfRenderedRows, numberOfRenderedColumns];

        // The row enumeration column is always at least 9 characters wide (Row \ Col)
        var maxColumnWidth = new int[numberOfRenderedColumns];
        maxColumnWidth[0] = 9;

        // Add header row
        for (var c = 1; c < numberOfRenderedColumns; c++)
        {
            table[0, c] = GetColumnName(cells.ColumnFirst + c - 1);
            maxColumnWidth[c] = table[0, c].Length;
        }

        // Add enumeration column
        for (var r = 0; r < numberOfRows; r++)
        {
            table[r + 1, 0] = GetRowName(cells.RowFirst + r);
        }

        // Parse cells and track empty rows, empty columns, and max column width along the way
        var rowsWithValues = new List<int>();
        var columnsWithValues = new List<int>();

        for (var r = 0; r < numberOfRows; r++)
        {
            for (var c = 0; c < numberOfColumns; c++)
            {
                if (values[r, c] is ExcelError excelError)
                {
                    throw new ExcelErrorException(excelError);
                }

                if (values[r, c] is ExcelEmpty)
                {
                    values[r, c] = string.Empty;
                }

                var value = values[r, c].ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(value))
                {
                    if (!rowsWithValues.Contains(r + 1))
                    {
                        rowsWithValues.Add(r + 1);
                    }

                    if (!columnsWithValues.Contains(c + 1))
                    {
                        columnsWithValues.Add(c + 1);
                    }
                }

                maxColumnWidth[c + 1] = Math.Max(maxColumnWidth[c + 1], value.Length);

                table[r + 1, c + 1] = value.Replace("\r\n", " ").Replace("\n", " ").Replace("|", "\\|");
            }
        }

        // Build the Markdown table
        var tableBuilder = new StringBuilder();

        // Render header row
        tableBuilder.Append("| Row \\ Col |");

        for (var c = 0; c < columnsWithValues.Count; c++)
        {
            tableBuilder.Append(' ');
            tableBuilder.Append(table[0, columnsWithValues[c]].PadRight(maxColumnWidth[columnsWithValues[c]]));
            tableBuilder.Append(" |");
        }

        tableBuilder.AppendLine();

        // Render header separator
        tableBuilder.Append("| ");
        tableBuilder.Append(new string('-', maxColumnWidth[0]));
        tableBuilder.Append(" |");

        for (var c = 0; c < columnsWithValues.Count; c++)
        {
            tableBuilder.Append(' ');
            tableBuilder.Append(new string('-', maxColumnWidth[columnsWithValues[c]]));
            tableBuilder.Append(" |");
        }

        tableBuilder.AppendLine();

        // Render cells
        for (var r = 0; r < rowsWithValues.Count; r++)
        {
            tableBuilder.Append('|');

            // Render row enumeration
            tableBuilder.Append(' ');
            tableBuilder.Append(table[rowsWithValues[r], 0].PadRight(maxColumnWidth[0]));
            tableBuilder.Append(" |");

            // Render row
            for (var c = 0; c < columnsWithValues.Count; c++)
            {
                tableBuilder.Append(' ');
                tableBuilder.Append(table[rowsWithValues[r], columnsWithValues[c]].PadRight(maxColumnWidth[columnsWithValues[c]]));
                tableBuilder.Append(" |");
            }

            tableBuilder.AppendLine();
        }

        if (rowsWithValues.Count == 0)
        {
            return $"The user has provided the cell range " +
                   $"{GetColumnName(cells.ColumnFirst)}{GetRowName(cells.RowFirst)}:" +
                   $"{GetColumnName(cells.ColumnFirst + values.GetLength(1) - 1)}{GetRowName(cells.RowFirst + values.GetLength(0) - 1)}, " +
                   $"but all cells are empty.";
        }

        return tableBuilder.ToString();
    }

    private static double ParseTemperature(double temperature)
    {
        if (temperature < 0 || temperature > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature argument must be between 0 and 1");
        }

        return temperature;
    }

    private static string? ParseProvider(object? providerAndModel)
    {
        if (providerAndModel is ExcelError excelError)
        {
            throw new ExcelErrorException(excelError);
        }

        return providerAndModel switch
        {
            string providerAndModelAsString => GetProvider(providerAndModelAsString),
            ExcelReference providerAndModelAsExcelReference => GetProvider(GetCellAsString(providerAndModelAsExcelReference)),
            null => null,  // Set to default
            _ => throw new ArgumentException("Provider and model argument must be a string or single cell")
        };
    }

    private static string GetProvider(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[..index];
    }

    private static string? ParseModel(object? providerAndModel)
    {
        if (providerAndModel is ExcelError excelError)
        {
            throw new ExcelErrorException(excelError);
        }

        return providerAndModel switch
        {
            string providerAndModelAsString => GetModel(providerAndModelAsString),
            ExcelReference providerAndModelAsExcelReference => GetModel(GetCellAsString(providerAndModelAsExcelReference)),
            null => null,  // Set to default
            _ => throw new ArgumentException("Provider and model argument must be a string or single cell")
        };
    }

    private static string GetModel(string providerAndModel)
    {
        var index = providerAndModel.IndexOf('/');

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[(index + 1)..];
    }

    private static string GetCellAsString(ExcelReference providerAndModel)
    {
        if (providerAndModel.RowFirst != providerAndModel.RowLast ||
            providerAndModel.ColumnFirst != providerAndModel.ColumnLast)
        {
            throw new ArgumentException("Provider and model argument must be a string or a single cell");
        }

        return providerAndModel.GetValue()?.ToString() ?? throw new ArgumentException("Provider and model argument must be a valid cell reference");
    }

    internal static string GetColumnName(int columnNumber)
    {
        var columnName = string.Empty;

        while (columnNumber >= 0)
        {
            columnName = (char)('A' + columnNumber % 26) + columnName;
            columnNumber = columnNumber / 26 - 1;
        }

        return columnName;
    }

    internal static string GetRowName(int rowNumber)
    {
        return (rowNumber + 1).ToString();
    }
}