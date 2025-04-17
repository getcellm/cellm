using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;

namespace Cellm.AddIn;

public class ArgumentParser
{
    private string? _provider;
    private string? _model;
    private object? _instructionsOrContext;
    private object? _instructionsOrTemperature;
    private object? _temperature;

    public static readonly string ContextStartTag = "<context>";
    public static readonly string ContextEndTag = "</context>";
    public static readonly string InstructionsStartTag = "<instructions>";
    public static readonly string InstructionsEndTag = "<instructions>";

    private readonly IConfiguration _configuration;

    public ArgumentParser(IConfiguration configuration)
    {
        _configuration = configuration;
    }

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

    public ArgumentParser AddInstructionsOrContext(object instructionsOrContext)
    {
        _instructionsOrContext = instructionsOrContext;

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

    public Arguments Parse()
    {
        var providerAsString = _provider ?? _configuration
            .GetSection(nameof(ProviderConfiguration))
            .GetValue<string>(nameof(ProviderConfiguration.DefaultProvider))
            ?? throw new ArgumentException(nameof(ProviderConfiguration.DefaultProvider));

        if (!Enum.TryParse<Provider>(providerAsString, true, out var provider))
        {
            throw new ArgumentException($"Unsupported default provider: {providerAsString}");
        }

        var model = _model ?? _configuration
            .GetSection($"{provider}Configuration")
            .GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        var defaultTemperature = _configuration
            .GetSection(nameof(ProviderConfiguration))
            .GetValue<double?>(nameof(ProviderConfiguration.DefaultTemperature))
            ?? throw new ArgumentException(nameof(ProviderConfiguration.DefaultTemperature));

        return (_instructionsOrContext, _instructionsOrTemperature, _temperature) switch
        {
            // "=PROMPT("Extract keywords")
            (string instructions, ExcelMissing, ExcelMissing) => new Arguments(provider, model, string.Empty, RenderInstructions(instructions), ParseTemperature(defaultTemperature)),
            // "=PROMPT("Extract keywords", 0.7)
            (string instructions, double temperature, ExcelMissing) => new Arguments(provider, model, string.Empty, RenderInstructions(instructions), ParseTemperature(temperature)),
            // "=PROMPT(A1:B2)
            (ExcelReference context, ExcelMissing, ExcelMissing) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(SystemMessages.InlineInstructions), ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, 0.7)
            (ExcelReference context, double temperature, ExcelMissing) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(SystemMessages.InlineInstructions), ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, "Extract keywords")
            (ExcelReference context, string instructions, ExcelMissing) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(instructions), ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, "Extract keywords", 0.7)
            (ExcelReference context, string instructions, double temperature) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(instructions), ParseTemperature(temperature)),
            // "=PROMPT(A1:B2, C1:D2)
            (ExcelReference context, ExcelReference instructions, ExcelMissing) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(ParseCells(instructions)), ParseTemperature(defaultTemperature)),
            // "=PROMPT(A1:B2, C1:D2, 0.7)
            (ExcelReference context, ExcelReference instructions, double temperature) => new Arguments(provider, model, RenderCells(ParseCells(context)), RenderInstructions(ParseCells(instructions)), ParseTemperature(temperature)),
            // Anything else
            _ => throw new ArgumentException($"Invalid arguments ({_instructionsOrContext?.GetType().Name}, {_instructionsOrTemperature?.GetType().Name}, {_temperature?.GetType().Name})")
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

    private static string ParseCells(ExcelReference reference)
    {
        try
        {
            var app = ExcelDnaUtil.Application;
            var sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, reference);
            sheetName = sheetName[(sheetName.LastIndexOf(']') + 1)..];
            var worksheet = app.Sheets[sheetName];

            var tableBuilder = new StringBuilder();
            var valueBuilder = new StringBuilder();

            var rows = reference.RowLast - reference.RowFirst + 1;
            var columns = reference.ColumnLast - reference.ColumnFirst + 1;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var value = worksheet.Cells[reference.RowFirst + row + 1, reference.ColumnFirst + column + 1].Text;
                    valueBuilder.Append(value);

                    tableBuilder.Append("| ");
                    tableBuilder.Append(GetColumnName(reference.ColumnFirst + column) + GetRowName(reference.RowFirst + row));
                    tableBuilder.Append(' ');
                    tableBuilder.Append(value);
                    tableBuilder.Append(' ');
                }

                tableBuilder.AppendLine("|");
            }

            if (string.IsNullOrEmpty(valueBuilder.ToString()))
            {
                throw new ArgumentException("Empty cells");
            }

            return tableBuilder.ToString();
        }
        catch (Exception ex)
        {
            throw new CellmException($"Failed to parse context: {ex.Message}", ex);
        }
    }

    private static string GetColumnName(int columnNumber)
    {
        string columnName = "";
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

    private static string RenderCells(string context)
    {
        return new StringBuilder()
            .AppendLine(ContextStartTag)
            .AppendLine(context)
            .AppendLine(ContextEndTag)
            .ToString();
    }

    private static string RenderInstructions(string instructions)
    {
        return new StringBuilder()
            .AppendLine(InstructionsStartTag)
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
}