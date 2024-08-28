using System.Text;
using Cellm.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public record Arguments(string Context, string Instructions, double Temperature);

public class ArgumentParser
{
    private ExcelReference? context;
    private string? instructions;
    private double temperature;

    public ArgumentParser(IOptions<CellmConfiguration> cellmConfiguration)
    {
        temperature = cellmConfiguration.Value.DefaultTemperature;
    }

    public ArgumentParser AddContext(object cellsArg)
    {
        if (cellsArg is ExcelReference excelRef)
        {
            context = excelRef;
        }
        else
        {
            throw new ArgumentException("First argument must be a cell (A1) or a range of cells (A1:B2).", nameof(cellsArg));
        }
        return this;
    }

    public ArgumentParser AddInstructionsOrTemperature(object arg)
    {
        if (arg is ExcelMissing)
        {
            return this;
        }

        switch (arg)
        {
            case string instructionsArg:
                instructions = instructionsArg;
                break;
            case double tempArg:
                AddTemperature(tempArg);
                break;
            default:
                throw new ArgumentException("Second argument must be either a string (instructions) or a double (temperature).");
        }

        return this;
    }

    public ArgumentParser AddTemperature(object temperatureArg)
    {
        if (temperatureArg is ExcelMissing)
        {
            return this;
        }

        if (temperatureArg is double tempArg)
        {
            if (tempArg < 0 || tempArg > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tempArg), "Temperature must be between 0 and 1");
            }

            temperature = tempArg;
        }
        else
        {
            throw new ArgumentException("Third argument must be a double (temperature).", nameof(temperatureArg));
        }

        return this;
    }

    public Arguments Parse()
    {
        if (context == null)
        {
            throw new InvalidOperationException("Cells are required to build a prompt.");
        }

        // Parse cells
        var cellsBuilder = new StringBuilder();
        cellsBuilder.AppendLine("<cells>");
        cellsBuilder.Append(Format.Cells(context));
        cellsBuilder.AppendLine("<cells>");

        // Parse instructions
        var instructionsBuilder = new StringBuilder();
        instructionsBuilder.AppendLine("<instructions>");

        if (string.IsNullOrEmpty(instructions))
        {
            instructionsBuilder.AppendLine("Analyze the cells carefully and follow any instructions within the table.");
        }
        else
        {
            instructionsBuilder.AppendLine(instructions);
        }

        instructionsBuilder.AppendLine("</instructions>");

        return new Arguments(cellsBuilder.ToString(), instructionsBuilder.ToString(), temperature);
    }
}