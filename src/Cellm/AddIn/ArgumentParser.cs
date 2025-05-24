using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;

namespace Cellm.AddIn;

public class ArgumentParser(IConfiguration configuration)
{
    private string? _provider;
    private string? _model;
    private object? _instructionsOrCells;
    private object? _instructionsOrTemperature;
    private object? _temperature;

    public static readonly string CellsBeginTag = "<cells>";
    public static readonly string CellsEndTag = "</cells>";
    public static readonly string InstructionsBeginTag = "<instructions>";
    public static readonly string InstructionsEndTag = "<instructions>";

    public ArgumentParser AddProvider(object providerAndModel)
    {
        _provider = providerAndModel switch
        {
            string providerAndModelAsString => GetProvider(providerAndModelAsString),
            ExcelReference providerAndModelAsExcelReference => GetProvider(GetCellAsString(providerAndModelAsExcelReference)),
            _ => throw new ArgumentException("Provider and model argument must be a string or single cell")
        };

        return this;
    }

    public ArgumentParser AddModel(object providerAndModel)
    {
        _model = providerAndModel switch
        {
            string providerAndModelAsString => GetModel(providerAndModelAsString),
            ExcelReference providerAndModelAsExcelReference => GetModel(GetCellAsString(providerAndModelAsExcelReference)),
            _ => throw new ArgumentException("Provider and model argument must be a string or single cell")
        };

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

    internal Arguments Parse()
    {
        var providerAsString = _provider ?? configuration
            .GetSection(nameof(ProviderConfiguration))
            .GetValue<string>(nameof(ProviderConfiguration.DefaultProvider))
            ?? throw new ArgumentException(nameof(ProviderConfiguration.DefaultProvider));

        if (!Enum.TryParse<Provider>(providerAsString, true, out var provider))
        {
            throw new ArgumentException($"Unsupported provider: {providerAsString}");
        }

        var model = _model ?? configuration
            .GetSection($"{provider}Configuration")
            .GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        var defaultTemperature = configuration
            .GetSection(nameof(ProviderConfiguration))
            .GetValue<double?>(nameof(ProviderConfiguration.DefaultTemperature))
            ?? throw new ArgumentException(nameof(ProviderConfiguration.DefaultTemperature));

        // Just copy values to unblock Excel's main thread thread as quickly as possible.
        // Cells will be rendered at a later stage in a background thread
        return (_instructionsOrCells, _instructionsOrTemperature, _temperature) switch
        {
            // "=PROMPT("Extract keywords")
            (string instructions, ExcelMissing, ExcelMissing) => new Arguments(provider, model, null, instructions, ParseTemperature(defaultTemperature)),
            // "=PROMPT("Extract keywords", 0.7)
            (string instructions, double temperature, ExcelMissing) => new Arguments(provider, model, null, instructions, ParseTemperature(temperature)),
            // "=PROMPT(A1:B2)
            (ExcelReference cells, ExcelMissing, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (string[,])cells.GetValue()), SystemMessages.InlineInstructions, ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, 0.7)
            (ExcelReference cells, double temperature, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (object[,])cells.GetValue()), SystemMessages.InlineInstructions, ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, "Extract keywords")
            (ExcelReference cells, string instructions, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (object[,])cells.GetValue()), instructions, ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, "Extract keywords", 0.7)
            (ExcelReference cells, string instructions, double temperature) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (object[,])cells.GetValue()), instructions, ParseTemperature(temperature)),
            // "=PROMPT(A1:B2, C1:D2)
            (ExcelReference cells, ExcelReference instructions, ExcelMissing) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (object[,])cells.GetValue()), new Cells(instructions.RowFirst, instructions.ColumnFirst, (object[,])instructions.GetValue()), ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, C1:D2, 0.7)
            (ExcelReference cells, ExcelReference instructions, double temperature) => new Arguments(provider, model, new Cells(cells.RowFirst, cells.ColumnFirst, (object[,])cells.GetValue()), new Cells(instructions.RowFirst, instructions.ColumnFirst, (object[,])instructions.GetValue()), ParseTemperature(temperature)),
            // Anything else
            _ => throw new ArgumentException($"Invalid arguments ({_instructionsOrCells?.GetType().Name}, {_instructionsOrTemperature?.GetType().Name}, {_temperature?.GetType().Name})")
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

    // Render sheet as Markdown table because models have seen loads of those
    internal static string ParseCells(Cells cells)
    {
        try
        {
            var numberOfRows = cells.Values.GetLength(0);
            var numberOfColumns = cells.Values.GetLength(1);

            var numberOfRenderedRows = numberOfRows + 1; // Includes the header row
            var numberOfRenderedColumns = numberOfColumns + 1; // Includes the row enumeration column

            
            var table = new string[numberOfRenderedRows, numberOfRenderedColumns];
            var isEmpty = true;

            // Add row number column
            table[0, 0] = string.Empty;

            for (var r = 0; r < numberOfRows; r++)
            {
                table[r + 1, 0] = GetRowName(cells.RowFirst + r);  // Skip header row
            }

            // Add other columns
            for (var c = 0; c < numberOfColumns; c++)
            {
                table[0, c + 1] = GetColumnName(cells.ColumnFirst + c);  // Skip row enumeration column

                for (var r = 0; r < numberOfRows; r++)
                {
                    if (cells.Values[r, c] is ExcelEmpty)
                    {
                        cells.Values[r, c] = string.Empty;
                    }

                    var value = cells.Values[r, c].ToString() ?? string.Empty;

                    if (isEmpty && !string.IsNullOrEmpty(value))
                    {
                        isEmpty = false;
                    }

                    var sanitizedValue = value.Replace("\r\n", " ").Replace("\n", " ").Replace("|", "\\|");
                    table[r + 1, c + 1] = sanitizedValue;
                }
            }

            // Pad columns
            for (var c = 0; c < numberOfRenderedColumns; c++)
            {
                var maxWidth = 0;

                for (var r = 0; r < numberOfRenderedRows; r++)
                {
                    maxWidth = Math.Max(maxWidth, table[r, c].Length);
                }

                for (var r = 0; r < numberOfRenderedRows; r++)
                {
                    table[r, c] = table[r, c].PadRight(maxWidth);
                }
            }

            var tableBuilder = new StringBuilder();

            // Iterate row-major for StringBuilder
            for (var r = 0; r < numberOfRenderedRows; r++)
            {
                tableBuilder.Append('|');

                for (var c = 0; c < numberOfRenderedColumns; c++)
                {
                    tableBuilder.Append(' ');
                    tableBuilder.Append(table[r, c]);
                    tableBuilder.Append(" |");
                }

                tableBuilder.AppendLine();

                // Add separator line after the header row (r == 0)
                if (r == 0)
                {
                    tableBuilder.Append('|');
                    for (var c = 0; c < numberOfRenderedColumns; c++)
                    {
                        tableBuilder.Append(' ');
                        // Length of separator is based on the padded header cell's length
                        tableBuilder.Append(new string('-', table[0, c].Length));
                        tableBuilder.Append(" |");
                    }

                    tableBuilder.AppendLine();
                }
            }

            if (isEmpty)
            {
                throw new ArgumentException($"{nameof(cells)} are empty");
            }

            return tableBuilder.ToString();
        }
        catch (Exception ex)
        {
            throw new CellmException($"Failed to parse context: {ex.Message}", ex);
        }
    }

    internal static string RenderCells(string cells)
    {
        return new StringBuilder()
            .AppendLine(CellsBeginTag)
            .AppendLine(cells)
            .AppendLine(CellsEndTag)
            .ToString();
    }

    internal static string RenderInstructions(string instructions)
    {
        return new StringBuilder()
            .AppendLine(InstructionsBeginTag)
            .AppendLine(instructions)
            .AppendLine(InstructionsEndTag)
            .ToString();
    }

    private static double ParseTemperature(double temperature)
    {
        if (temperature < 0 || temperature > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature argument must be between 0 and 1");
        }

        return temperature;
    }

    private static string GetColumnName(int columnNumber)
    {
        var columnName = string.Empty;

        while (columnNumber >= 0)
        {
            columnName = (char)('A' + columnNumber % 26) + columnName;
            columnNumber = columnNumber / 26 - 1;
        }

        return columnName;
    }

    private static string GetRowName(int rowNumber)
    {
        return (rowNumber + 1).ToString();
    }
}