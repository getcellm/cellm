using System.Diagnostics;
using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public static class CellmFunctions
{
    private const string _promptDescription = "Sends a prompt to the default model. Can be used with or without a cell range as context.";
    private const string _promptModelDescription = "Sends a prompt to the specified model. Can be used with or without a cell range as context.";

    private const string _structuredOutputShapeRowDescription = " Multiple output values spill into cells to the right.";
    private const string _structuredOutputShapeColumnDescription = " Multiple output values spill into cells below.";
    private const string _structuredOutputShapeRangeDescription = " The model chooses whether multiple output values should spill into rows and/or columns or not.";

    private const string _promptExample = $"""
         Example:

        =PROMPT(A1:B2, "Extract named entities", 0.7)
        """;

    private const string _promptModelExample = """
        Example:

        =PROMPT("openai/gpt-4.1", A1:B2, "Extract named entities", 0.7)
        """;

    private const string _instructionsOrCellsName = "Prompt or context";
    private const string _instructionsOrCellsDescription = "A prompt (string) or context for the prompt in the next argument (a cell or cell range e.g., A1:B2).";

    private const string _instructionsOrTemperatureName = "Prompt or temperature";
    private const string _instructionsOrTemperatureDescription = "(Optional) A prompt when the first argument is a cell range with context (string, cell, or cell range e.g., A1:B2) else the model's temperature (0.0 - 1.0)";

    private const string _temperatureName = "Temperature";
    private const string _temperatureDescription = "(Optional) The model's temperature (0.0 - 1.0) when the second argument is a prompt.";

    private const string _providerAndModelName = "Provider and model";
    private const string _promptAndModelDescription = @"The provider and model on the form ""{provider}/{model}"" (e.g., openai/gpt-4.1)";


    /// <summary>
    /// Sends a prompt to the default model configured in CellmConfiguration.
    /// </summary>
    /// <param name="instructionsOrCells">
    /// A prompt (string) or context (cell or cell range).
    /// </param>
    /// <param name="instructionsOrTemperature">
    /// A prompt (string, cell, or cell range) or a temperature value.
    /// If prompt is omitted, any instructions found in the cells of the first argument will be used as instructions.
    /// </param>
    /// <param name="temperature">
    /// A value between 0 and 1 that controls the randomness of the model's output.
    /// Lower values make the output more deterministic, higher values make it more random.
    /// </param>
    /// <returns>
    /// The model's response as a string. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPT", Description = _promptDescription + _promptExample, IsThreadSafe = true, IsVolatile = false)]
    public static object Prompt(
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return Run(
            $"{provider}/{model}",
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.None);
    }

    /// <summary>
    /// Same as Prompt, but multiple output values spill into cells to the right.
    /// </returns>
    [ExcelFunction(Name = "PROMPT.TOROW", Description = _promptDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToRow(
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return Run(
            $"{provider}/{model}",
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Row);
    }

    /// <summary>
    /// Same as Prompt, but multiple output values spill into cells below.
    /// </returns>
    [ExcelFunction(Name = "PROMPT.TOCOLUMN", Description = _promptDescription + _structuredOutputShapeColumnDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToColumn(
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return Run(
            $"{provider}/{model}",
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Column);
    }

    /// <summary>
    /// Same as Prompt, but outputs to a single cell.
    /// </returns>
    [ExcelFunction(Name = "PROMPT.TORANGE", Description = _promptDescription + _structuredOutputShapeRangeDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToRange(
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return Run(
            $"{provider}/{model}",
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Range);
    }

    /// <summary>
    /// Sends a prompt to the specified model.
    /// </summary>
    /// <param name="instructionsOrCells">
    /// A prompt (string) or context (cell or cell range).
    /// </param>
    /// <param name="instructionsOrTemperature">
    /// A prompt (string, cell, or cell range) or a temperature value.
    /// If prompt is omitted, any instructions found in the cells of the first argument will be used as instructions.
    /// </param>
    /// <param name="temperature">
    /// A value between 0 and 1 that controls the randomness of the model's output.
    /// Lower values make the output more deterministic, higher values make it more random.
    /// </param>
    /// <returns>
    /// The model's response as a string. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPTMODEL", Description = _promptModelDescription + _promptModelExample, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModel(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        return Run(
            providerAndModel,
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.None);
    }

    /// <summary>
    /// Same as PromptModel, but multiple output values spill into cells to the right.
    /// </returns>
    [ExcelFunction(Name = "PROMPTMODEL.TOROW", Description = _promptModelDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToRow(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        return Run(
            providerAndModel,
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Row);
    }

    /// <summary>
    /// Same as PromptModel, but multiple output values spill into cells below.
    /// </returns>
    [ExcelFunction(Name = "PROMPTMODEL.TOCOLUMN", Description = _promptModelDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToColumn(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        return Run(
            providerAndModel,
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Column);
    }

    /// <summary>
    /// Same as PromptModel, but multiple values spill into a column.
    /// </returns>
    [ExcelFunction(Name = "PROMPTMODEL.TORANGE", Description = _promptModelDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToCell(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrCellsName, Description = _instructionsOrCellsDescription)] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = _instructionsOrTemperatureName, Description = _instructionsOrTemperatureDescription)] object instructionsOrTemperature,
        [ExcelArgument(Name = _temperatureName, Description = _temperatureDescription)] object temperature)
    {
        return Run(
            providerAndModel,
            instructionsOrCells,
            instructionsOrTemperature,
            temperature,
            StructuredOutputShape.Range);
    }

    /// <summary>
    /// Parses arguments on Excel's main thread and hands off the actual work to a background thread to avoid blocking Excel's main thread.
    /// </summary>
    public static object Run(object providerAndModel, object instructionsOrCells, object instructionsOrTemperature, object temperature, StructuredOutputShape outputShape)
    {
        if (ExcelDnaUtil.IsInFunctionWizard())
        {
            return "Click OK to generate result";
        }

        try
        {
            var wallClock = Stopwatch.StartNew();

            // We must parse arguments on the main thread
            var argumentParser = CellmAddIn.Services.GetRequiredService<ArgumentParser>();
            var arguments = argumentParser
                .AddProvider(providerAndModel)
                .AddModel(providerAndModel)
                .AddInstructionsOrCells(instructionsOrCells)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .AddOutputShape(outputShape)
                .Parse();

            var caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
            var callerCoordinates = $"{ArgumentParser.GetColumnName(caller?.ColumnFirst ?? 0)}{ArgumentParser.GetRowName(caller?.RowFirst ?? 0)}";

            var response = ExcelAsyncUtil.RunTaskWithCancellation(
                nameof(Run),
                new object[] { providerAndModel, instructionsOrCells, instructionsOrTemperature, temperature },
                cancellationToken => GetResponseAsync(arguments, wallClock, callerCoordinates, cancellationToken));

            if (response is ExcelError.ExcelErrorNA)
            {
                return ExcelError.ExcelErrorGettingData;
            }

            return response;
        }
        catch (ExcelErrorException ex)
        {
            // Short-circuit if any arguments were found to be #GETTING_DATA or contain other errors during argument parsing. 
            // Excel will re-trigger this function (or already has) when inputs are updated with realized values.
            return ex.GetExcelError();
        }
        catch (XlCallException)
        {
            // Could be many things but the only thing observed in practice is XlReturnUncalced, meaning an
            // ExcelReference's value wasn't ready yet
            return ExcelError.ExcelErrorGettingData;
        }

        // Deliberately omit catch (Exception ex) to let UnhandledExceptionHandler log unexpected exceptions
    }

    /// <summary>
    /// Builds a prompt, sends it to the model, and returns the response.
    /// </summary>
    internal static async Task<object> GetResponseAsync(Arguments arguments, Stopwatch wallClock, string callerCoordinates, CancellationToken cancellationToken)
    {
        var requestClock = Stopwatch.StartNew();

        var logger = CellmAddIn.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(GetResponseAsync));

        try
        {
            logger.LogInformation("Sending prompt to {}/{} ({}) ... (elapsed time: {}ms)", arguments.Provider, arguments.Model, callerCoordinates, wallClock.ElapsedMilliseconds);

            // Check for cancellation before doing any work
            cancellationToken.ThrowIfCancellationRequested();

            var elapsedTaskStart = wallClock.ElapsedMilliseconds;

            var cells = arguments.Cells switch
            {
                string singleCell => singleCell,
                Cells manyCells => ArgumentParser.ParseCells(manyCells),
                null => "Not available",
                _ => throw new ArgumentException(nameof(arguments.Cells))
            };

            var instructions = arguments.Instructions switch
            {
                string singleCell => singleCell,
                Cells manyCells => ArgumentParser.ParseCells(manyCells),
                _ => throw new ArgumentException(nameof(arguments.Instructions))
            };

            var userMessage = new StringBuilder()
                .AppendLine(ArgumentParser.AddInstructions(instructions))
                .AppendLine(ArgumentParser.AddCells(cells))
                .ToString();

            var cellmAddInConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmAddInConfiguration>>();

            var prompt = new PromptBuilder()
                .SetModel(arguments.Model)
                .SetTemperature(arguments.Temperature)
                .SetMaxOutputTokens(cellmAddInConfiguration.CurrentValue.MaxOutputTokens)
                .SetOutputShape(arguments.OutputShape)
                .AddSystemMessage(SystemMessages.SystemMessage(arguments.Provider, arguments.Model, DateTime.UtcNow))
                .AddUserMessage(userMessage)
                .Build();

            // Check for cancellation before sending request
            cancellationToken.ThrowIfCancellationRequested();

            var client = CellmAddIn.Services.GetRequiredService<Client>();
            var response = await client.GetResponseAsync(prompt, arguments.Provider, cancellationToken).ConfigureAwait(false);
            var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

            // Check for cancellation before returning response
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Sending prompt to {}/{} ({}) ... Done (elapsed time: {}ms, request time: {}ms)", arguments.Provider, arguments.Model, callerCoordinates, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            if (StructuredOutput.TryParse(assistantMessage, response.OutputShape, out var structuredAssistantMessage) && structuredAssistantMessage is not null)
            {
                return structuredAssistantMessage;
            }

            return assistantMessage;
        }
        // Short-circuit if any cells were found to be #GETTING_DATA or contain other errors during cell parsing. 
        // Excel will re-trigger this function (or already has) when inputs are updated with realized values.
        catch (ExcelErrorException ex)
        {
            return ex.GetExcelError();
        }
        catch (OperationCanceledException)
        {
            CellmAddIn.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(GetResponseAsync))
                .LogInformation("Sending prompt to {}/{} ({}) ... Cancelled (elapsed time: {}ms, request time: {}ms)", arguments.Provider, arguments.Model, callerCoordinates, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return "Cancelled"; // Cancellation is not an error, just return _something_
        }
        catch (CellmException ex)
        {
            CellmAddIn.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(GetResponseAsync))
                .LogError(ex, "Sending prompt to {}/{} ({}) ... Failed: {message} (elapsed time: {}ms, request time: {}ms)", arguments.Provider, arguments.Model, callerCoordinates, ex.Message, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return ex.Message;
        }

        // Deliberately omit catch (Exception ex) to let UnhandledExceptionHandler log unexpected exceptions.
    }
}
