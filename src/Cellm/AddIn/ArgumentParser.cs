using System.Text;
using Cellm.AddIn.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public record Arguments(string Provider, string Model, string Context, string Instructions, double Temperature);

public class ArgumentParser
{
    private string _provider;
    private string _model;
    private string? _context;
    private string? _instructions;
    private double _temperature;

    public ArgumentParser(IOptions<CellmConfiguration> cellmConfiguration)
    {
        _provider = cellmConfiguration.Value.DefaultModelProvider;
        _model = cellmConfiguration.Value.DefaultModel;
        _temperature = cellmConfiguration.Value.DefaultTemperature;
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
            _context = Format.Cells(contextAsExcelReference);
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
                _instructions = Format.Cells(instructionsOrTemperatureAsExcelReference);
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
        if (_context == null)
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

        return new Arguments(_provider, _model, contextBuilder.ToString(), instructionsBuilder.ToString(), _temperature);
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
}