using System.Text;
using Cellm.Prompts;
using Cellm.Services.Configuration;
using ExcelDna.Integration;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public record Arguments(string Context, string Instructions, double Temperature);

public class ArgumentParser
{
    private string? context;
    private string? instructions;
    private double temperature;

    public ArgumentParser(IOptions<CellmAddInConfiguration> cellmConfiguration)
    {
        temperature = cellmConfiguration.Value.DefaultTemperature;
    }

    public ArgumentParser AddContext(object cellsArg)
    {
        if (cellsArg is ExcelReference excelRef)
        {
            context = Format.Cells(excelRef);
        }
        else
        {
            throw new ArgumentException("First argument must be a cell (A1) or a range of cells (A1:B2).", nameof(cellsArg));
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
            case string i:
                instructions = i;
                break;
            case ExcelReference excelRef:
                instructions = Format.Cells(excelRef);
                break;
            case double t:
                AddTemperature(t);
                break;
            default:
                throw new ArgumentException("Second argument must be a cell, a range of cells, or a string (instructions) or a double (temperature).");
        }

        return this;
    }

    public ArgumentParser AddTemperature(object Temperature)
    {
        if (Temperature is ExcelMissing)
        {
            return this;
        }

        if (Temperature is double t)
        {
            if (t < 0 || t > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Temperature), "Temperature must be between 0 and 1");
            }

            temperature = t;
        }
        else
        {
            throw new ArgumentException("Third argument must be a double (temperature).", nameof(Temperature));
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
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("<context>");
        contextBuilder.Append(context);
        contextBuilder.AppendLine("<context>");

        // Parse instructions
        var instructionsBuilder = new StringBuilder();
        instructionsBuilder.AppendLine("<instructions>");

        if (string.IsNullOrEmpty(instructions))
        {
            instructionsBuilder.AppendLine(CellmPrompts.InlineInstructions);
        }
        else
        {
            instructionsBuilder.AppendLine(instructions);
        }

        instructionsBuilder.AppendLine("</instructions>");

        return new Arguments(contextBuilder.ToString(), instructionsBuilder.ToString(), temperature);
    }
}