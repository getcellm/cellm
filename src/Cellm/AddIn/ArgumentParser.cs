using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Services.Configuration;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Office.Interop.Excel;

namespace Cellm.AddIn;

public record Arguments(string Provider, string Model, string Context, string Instructions, double Temperature);

public class ArgumentParser
{
    private string? _provider;
    private string? _model;
    private string? _context;
    private string? _instructions;
    private double? _temperature;

    private readonly IConfiguration _configuration;

    public ArgumentParser(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ArgumentParser AddProvider(object providerAndModel)
    {
        switch (providerAndModel)
        {
            case string providerAndModelAsString:
                _provider = GetProvider(providerAndModelAsString);
                break;
            case ExcelReference providerAndModelAsExcelReference:
                if (providerAndModelAsExcelReference.RowFirst != providerAndModelAsExcelReference.RowLast ||
                    providerAndModelAsExcelReference.ColumnFirst != providerAndModelAsExcelReference.ColumnLast)
                {
                    throw new ArgumentException("Provider argument must be a single cell");
                }

                var providerAndModelToString = providerAndModelAsExcelReference.GetValue()?.ToString()
                    ?? throw new ArgumentException("Provider argument must be a valid cell reference");
                _model = GetModel(providerAndModelToString);
                break;
            default:
                throw new ArgumentException("Provider argument must be a cell or a string");
        }

        return this;
    }

    public ArgumentParser AddModel(object providerAndModel)
    {
        switch (providerAndModel)
        {
            case string providerAndModelAsString:
                _model = GetModel(providerAndModelAsString);
                break;
            case ExcelReference providerAndModelAsExcelReference:
                if (providerAndModelAsExcelReference.RowFirst != providerAndModelAsExcelReference.RowLast ||
                    providerAndModelAsExcelReference.ColumnFirst != providerAndModelAsExcelReference.ColumnLast)
                {
                    throw new ArgumentException("Model argument argument must be a single cell");
                }

                var providerAndModelToString = providerAndModelAsExcelReference.GetValue()?.ToString() ?? throw new ArgumentException("Model argument must be a valid cell reference");
                _model = GetModel(providerAndModelToString);
                break;
            default:
                throw new ArgumentException("Model argument must be a cell or a string");
        }

        return this;
    }

    public ArgumentParser AddContext(object context)
    {
        if (context is ExcelReference contextAsExcelReference)
        {
            _context = FormatCells(contextAsExcelReference);
        }
        else
        {
            throw new ArgumentException("Context argument must be a cell or a range of cells", nameof(context));
        }

        return this;
    }

    public ArgumentParser AddInstructionsOrTemperature(object instructionsOrTemperature)
    {
        if (instructionsOrTemperature is ExcelMissing)
        {
            return this;
        }

        switch (instructionsOrTemperature)
        {
            case string instructionsOrTemperatureAsString:
                _instructions = instructionsOrTemperatureAsString;
                break;
            case ExcelReference instructionsOrTemperatureAsExcelReference:
                _instructions = FormatCells(instructionsOrTemperatureAsExcelReference);
                break;
            case double instructionsOrTemperatureAsDouble:
                AddTemperature(instructionsOrTemperatureAsDouble);
                break;
            default:
                throw new ArgumentException("InstructionsOrTemperature argument must be a cell, a range of cells, or a string (instructions) or a double (temperature).");
        }

        return this;
    }

    public ArgumentParser AddTemperature(object temperature)
    {
        if (temperature is ExcelMissing)
        {
            return this;
        }

        if (temperature is double temperatureAsDouble)
        {
            if (temperatureAsDouble < 0 || temperatureAsDouble > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature argument must be between 0 and 1");
            }

            _temperature = temperatureAsDouble;
        }
        else
        {
            throw new ArgumentException("Temperature argument must be a double (temperature).", nameof(temperature));
        }

        return this;
    }

    public Arguments Parse()
    {
        var provider = _configuration.GetSection(nameof(CellmConfiguration)).GetValue<string>(nameof(CellmConfiguration.DefaultProvider))
            ?? throw new ArgumentException(nameof(CellmConfiguration.DefaultProvider));

        if (!string.IsNullOrEmpty(_provider))
        {
            provider = _provider;
        }

        var model = _configuration.GetSection($"{provider}Configuration").GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        if (!string.IsNullOrEmpty(_model))
        {
            model = _model;
        }

        if (_context is null)
        {
            throw new InvalidOperationException("Context argument is required");
        }

        // Parse cells
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("<context>");
        contextBuilder.Append(_context);
        contextBuilder.AppendLine("</context>");

        // Parse instructions
        var instructionsBuilder = new StringBuilder();
        instructionsBuilder.AppendLine("<instructions>");

        if (string.IsNullOrEmpty(_instructions))
        {
            instructionsBuilder.AppendLine(CellmPrompts.InlineInstructions);
        }
        else
        {
            instructionsBuilder.AppendLine(_instructions);
        }

        instructionsBuilder.AppendLine("</instructions>");

        var temperature = _configuration.GetSection(nameof(CellmConfiguration)).GetValue<double>(nameof(CellmConfiguration.DefaultTemperature));

        if (_temperature is not null)
        {
            temperature = Convert.ToDouble(_temperature);
        }

        return new Arguments(provider, model, contextBuilder.ToString(), instructionsBuilder.ToString(), temperature);
    }

    private static string GetProvider(string providerAndModel)
    {
        var index = providerAndModel.IndexOf("/");

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[..index];
    }

    private static string GetModel(string providerAndModel)
    {
        var index = providerAndModel.IndexOf("/");

        if (index < 0)
        {
            throw new ArgumentException($"Provider and model argument must on the form \"Provider/Model\"");
        }

        return providerAndModel[(index + 1)..];
    }

    private static string FormatCells(ExcelReference reference)
    {
        try
        {
            var app = (Application)ExcelDnaUtil.Application;
            var sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, reference);
            sheetName = sheetName[(sheetName.LastIndexOf("]") + 1)..];
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
                throw new ArgumentException("Context cannot not be empty");
            }

            return tableBuilder.ToString();
        }
        catch (Exception ex)
        {
            throw new CellmException("Failed to format context: ", ex);
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
}