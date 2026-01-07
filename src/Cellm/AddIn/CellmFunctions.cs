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
    private const string _promptDescription = "Sends a prompt to the default model. Can be used with or without cell ranges as context.";
    private const string _promptModelDescription = "Sends a prompt to the specified model. Can be used with or without cell ranges as context.";

    private const string _structuredOutputShapeRowDescription = " Multiple output values spill into cells to the right.";
    private const string _structuredOutputShapeColumnDescription = " Multiple output values spill into cells below.";
    private const string _structuredOutputShapeRangeDescription = " The model chooses whether multiple output values should spill into rows and/or columns or not.";

    private const string _promptExample = $"""
         Example:

        =PROMPT("Extract named entities", A1:B2, C3:D4)
        """;

    private const string _promptModelExample = """
        Example:

        =PROMPTMODEL("openai/gpt-4.1", "Extract named entities", A1:B2, C3:D4)
        """;

    private const string _instructionsName = "Prompt";
    private const string _instructionsDescription = "The prompt to send to the model (string, cell, or cell range e.g., A1:B2).";

    private const string _cellsName = "Cells";
    private const string _cellsDescription = "(Optional) One or more cell ranges as context (e.g., A1, B2:C3).";

    private const string _providerAndModelName = "Provider and model";
    private const string _promptAndModelDescription = @"The provider and model on the form ""{provider}/{model}"" (e.g., openai/gpt-4.1)";


    /// <summary>
    /// Sends a prompt to the default model configured in CellmConfiguration.
    /// </summary>
    /// <param name="instructions">
    /// The prompt to send to the model (string, cell, or cell range).
    /// </param>
    /// <param name="cells">
    /// Optional cell ranges to provide as context for the prompt.
    /// </param>
    /// <returns>
    /// The model's response in a single cell. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPT", Description = _promptDescription + _promptExample, IsThreadSafe = true, IsVolatile = false)]
    public static object Prompt(
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            instructions,
            ranges,
            StructuredOutputShape.None);
    }

    /// <summary>
    /// Same as Prompt, but array response spill into cells to the right.
    /// </summary>
    [ExcelFunction(Name = "PROMPT.TOROW", Description = _promptDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToRow(
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            instructions,
            ranges,
            StructuredOutputShape.Row);
    }

    /// <summary>
    /// Same as Prompt, but array response spill into cells below.
    /// </summary>
    [ExcelFunction(Name = "PROMPT.TOCOLUMN", Description = _promptDescription + _structuredOutputShapeColumnDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToColumn(
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            instructions,
            ranges,
            StructuredOutputShape.Column);
    }

    /// <summary>
    /// Same as Prompt, but array response spill into rows and columns.
    /// </summary>
    [ExcelFunction(Name = "PROMPT.TORANGE", Description = _promptDescription + _structuredOutputShapeRangeDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptToRange(
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            instructions,
            ranges,
            StructuredOutputShape.Range);
    }

    /// <summary>
    /// Sends a prompt to the specified model.
    /// </summary>
    /// <param name="providerAndModel">
    /// The model identifier.
    /// </param>
    /// <param name="instructions">
    /// The prompt to send to the model (string, cell, or cell range).
    /// </param>
    /// <param name="cells">
    /// Optional cell ranges to provide as context for the prompt.
    /// </param>
    /// <returns>
    /// The model's response in a single cell. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPTMODEL", Description = _promptModelDescription + _promptModelExample, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModel(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            providerAndModel,
            instructions,
            ranges,
            StructuredOutputShape.None);
    }

    /// <summary>
    /// Same as PromptModel, but array response spill into cells to the right.
    /// </summary>
    [ExcelFunction(Name = "PROMPTMODEL.TOROW", Description = _promptModelDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToRow(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            providerAndModel,
            instructions,
            ranges,
            StructuredOutputShape.Row);
    }

    /// <summary>
    /// Same as PromptModel, but array response spill into cells below.
    /// </summary>
    [ExcelFunction(Name = "PROMPTMODEL.TOCOLUMN", Description = _promptModelDescription + _structuredOutputShapeRowDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToColumn(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            providerAndModel,
            instructions,
            ranges,
            StructuredOutputShape.Column);
    }

    /// <summary>
    /// Same as PromptModel, but array responses spill into rows and columns.
    /// </summary>
    [ExcelFunction(Name = "PROMPTMODEL.TORANGE", Description = _promptModelDescription, IsThreadSafe = true, IsVolatile = false)]
    public static object PromptModelToCell(
        [ExcelArgument(AllowReference = true, Name = _providerAndModelName, Description = _promptAndModelDescription)] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = _instructionsName, Description = _instructionsDescription)] object instructions,
        [ExcelArgument(AllowReference = true, Name = _cellsName, Description = _cellsDescription)] params object[] ranges)
    {
        return Run(
            providerAndModel,
            instructions,
            ranges,
            StructuredOutputShape.Range);
    }

    /// <summary>
    /// Forwards arguments along with the default provider and model.
    /// </summary>
    public static object Run(object instructions, object[] ranges, StructuredOutputShape outputShape)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return Run(
            $"{provider}/{model}",
            instructions,
            ranges,
            outputShape);
    }

    /// <summary>
    /// Parses arguments on Excel's main thread and hands off the actual work to a background thread to avoid blocking Excel's main thread.
    /// </summary>
    public static object Run(object providerAndModel, object instructions, object[] ranges, StructuredOutputShape outputShape)
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
                .AddInstructions(instructions)
                .AddCells(ranges)
                .AddOutputShape(outputShape)
                .Parse();

            var caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
            var callerCoordinates = $"{ArgumentParser.GetColumnName(caller?.ColumnFirst ?? 0)}{ArgumentParser.GetRowName(caller?.RowFirst ?? 0)}";

            // Do work on background thread, releasing Excel's main thread to keep UI responsive
            var response = ExcelAsyncUtil.RunTaskWithCancellation(
                nameof(Run),
                // Add callerCoordinates to make task arguments unique, otherwise all concurrent calls
                // with identical arguments will reuse the response from the first call that finishes.
                // ExcelDNA calls this function twice. Once when invoked and once when result is ready
                // at which point the list of arguments is used as key to pair result with first call
                new object[] { providerAndModel, instructions, ranges, callerCoordinates },
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
            logger.LogInformation("Sending {caller} to {provider}/{model} ...", callerCoordinates, arguments.Provider, arguments.Model);

            // Check for cancellation before doing any work
            cancellationToken.ThrowIfCancellationRequested();

            var elapsedTaskStart = wallClock.ElapsedMilliseconds;

            var instructions = arguments.Instructions switch
            {
                string cell => cell,
                Range range => ArgumentParser.RenderRange(range),
                _ => throw new ArgumentException(nameof(arguments.Instructions))
            };

            var ranges = ArgumentParser.RenderRanges(arguments.Ranges);

            var userMessage = new StringBuilder()
                .AppendLine(ArgumentParser.FormatInstructions(instructions))
                .AppendLine(ArgumentParser.FormatRanges(ranges))
                .ToString();

            var cellmAddInConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmAddInConfiguration>>();

            var prompt = new PromptBuilder()
                .SetModel(arguments.Model)
                .SetTemperature(arguments.Temperature)
                .SetMaxOutputTokens(cellmAddInConfiguration.CurrentValue.MaxOutputTokens)
                .SetOutputShape(arguments.OutputShape)
                .AddSystemMessage(SystemMessages.SystemMessage(arguments.Provider, arguments.Model, DateTime.Today))
                .AddUserMessage(userMessage)
                .Build();

            // Check for cancellation before sending request
            cancellationToken.ThrowIfCancellationRequested();

            var client = CellmAddIn.Services.GetRequiredService<Client>();
            var response = await client.GetResponseAsync(prompt, arguments.Provider, cancellationToken).ConfigureAwait(false);
            var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

            // Check for cancellation before returning response
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Sending {caller} to {provider}/{model} ... Done (elapsed time: {elapsedTime}ms, request time: {requestTime}ms, overhead: {overhead}ms)", callerCoordinates, arguments.Provider, arguments.Model, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds, wallClock.ElapsedMilliseconds - requestClock.ElapsedMilliseconds);

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
                .LogInformation("Sending {caller} to {provider}/{model} ... Cancelled (elapsed time: {elapsedTime}ms, request time: {requestTime}ms)", callerCoordinates, arguments.Provider, arguments.Model, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return "Cancelled"; // We must return _something_
        }
        catch (CellmException ex)
        {
            CellmAddIn.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(GetResponseAsync))
                .LogError(ex, "Sending {caller} to {provider}/{model} ... Failed: {message} (elapsed time: {elapsedTime}ms, request time: {requestTime}ms)", callerCoordinates, arguments.Provider, arguments.Model, ex.Message, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return ex.Message;
        }

        // Deliberately omit catch (Exception ex) to let UnhandledExceptionHandler log unexpected exceptions.
    }
}
