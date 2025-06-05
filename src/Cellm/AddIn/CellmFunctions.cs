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
    /// <summary>
    /// Sends a prompt to the default model configured in CellmConfiguration.
    /// </summary>
    /// <param name="context">A cell or range of cells containing the context for the prompt.</param>
    /// <param name="instructionsOrTemperature">
    /// A cell or range of cells with instructions, or a temperature value.
    /// If omitted, any instructions found in the context will be used.
    /// </param>
    /// <param name="temperature">
    /// A value between 0 and 1 that controls the randomness of the model's output.
    /// Lower values make the output more deterministic, higher values make it more random.
    /// </param>
    /// <returns>
    /// The model's response as a string. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPT", Description = "Send a prompt to the default model", IsThreadSafe = true, IsVolatile = false)]
    public static object Prompt(
    [ExcelArgument(AllowReference = true, Name = "InstructionsOrContext", Description = "A string with instructions or a cell or range of cells with context")] object context,
    [ExcelArgument(AllowReference = true, Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
    [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration[$"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.DefaultProvider)}"]
            ?? throw new ArgumentException(nameof(CellmAddInConfiguration.DefaultProvider));

        var model = configuration[$"{provider}Configuration:{nameof(IProviderConfiguration.DefaultModel)}"]
            ?? throw new ArgumentException(nameof(IProviderConfiguration.DefaultModel));

        return PromptWith(
                   $"{provider}/{model}",
                   context,
                   instructionsOrTemperature,
                   temperature);
    }

    /// <summary>
    /// Sends a prompt to the specified model.
    /// </summary>
    /// <param name="providerAndModel">The provider and model in the format "provider/model".</param>
    /// <param name="instructionsOrCells">A string with instructions or a cell or range of cells with context.</param>
    /// <param name="instructionsOrTemperature">
    /// A cell or range of cells with instructions, or a temperature value.
    /// If omitted, any instructions found in the context will be used.
    /// </param>
    /// <param name="temperature">
    /// A value between 0 and 1 that controls the randomness of the model's output.
    /// Lower values make the output more deterministic, higher values make it more random.
    /// </param>
    /// <returns>
    /// The model's response as a string. If an error occurs, it returns the error message.
    /// </returns>
    [ExcelFunction(Name = "PROMPTWITH", Description = "Send a prompt to a specific model", IsThreadSafe = true, IsVolatile = false)]
    public static object PromptWith(
        [ExcelArgument(AllowReference = true, Name = "Provider/Model")] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = "InstructionsOrContext", Description = "A string with instructions or a cell or range of cells with context")] object instructionsOrCells,
        [ExcelArgument(AllowReference = true, Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
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
                .Parse();

            var caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
            var callerCoordinates = $"{ArgumentParser.GetColumnName(caller?.ColumnFirst ?? 0)}{ArgumentParser.GetRowName(caller?.RowFirst ?? 0)}";

            var response = ExcelAsyncUtil.RunTaskWithCancellation(
                nameof(PromptWith),
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

        // Deliberately omit catch (Exception ex) to let UnhandledExceptionHandler log unexpected exceptions.
    }

    internal static async Task<object> GetResponseAsync(Arguments arguments, Stopwatch wallClock, string callerCoordinates, CancellationToken cancellationToken)
    {
        var requestClock = Stopwatch.StartNew();

        var logger = CellmAddIn.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(GetResponseAsync));

        try
        {
            logger.LogInformation("Sending prompt ({}) ... (elapsed time: {}ms)", callerCoordinates, wallClock.ElapsedMilliseconds);

            // Check for cancellation before doing any work
            cancellationToken.ThrowIfCancellationRequested();

            var elapsedTaskStart = wallClock.ElapsedMilliseconds;

            var cells = arguments.Cells switch
            {
                Cells argumentCells => ArgumentParser.ParseCells(argumentCells),
                null => "Not available",
                _ => throw new ArgumentException(nameof(arguments.Cells))
            };

            var instructions = arguments.Instructions switch
            {
                string argumentInstruction => argumentInstruction,
                Cells argumentInstruction => ArgumentParser.ParseCells(argumentInstruction),
                _ => throw new ArgumentException(nameof(arguments.Instructions))
            };

            var userMessage = new StringBuilder()
                .AppendLine(ArgumentParser.AddInstructionTags(instructions))
                .AppendLine(ArgumentParser.AddCellTags(cells))
                .ToString();

            var cellmAddInConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmAddInConfiguration>>();

            var prompt = new PromptBuilder()
                .SetModel(arguments.Model)
                .SetTemperature(arguments.Temperature)
                .SetMaxOutputTokens(cellmAddInConfiguration.CurrentValue.MaxOutputTokens)
                .AddSystemMessage(SystemMessages.SystemMessage)
                .AddUserMessage(userMessage)
                .Build();

            // Check for cancellation before sending request
            cancellationToken.ThrowIfCancellationRequested();

            var client = CellmAddIn.Services.GetRequiredService<Client>();
            var response = await client.GetResponseAsync(prompt, arguments.Provider, cancellationToken).ConfigureAwait(false);
            var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

            logger.LogInformation("Sending prompt ({}) ... Done (elapsed time: {}ms, request time: {}ms)", callerCoordinates, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

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
                .LogInformation("Sending prompt ({}) ... Cancelled (elapsed time: {}ms, request time: {}ms)", callerCoordinates, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return "Cancelled"; // Cancellation is not an error, just return _something_
        }
        catch (CellmException ex)
        {
            CellmAddIn.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(GetResponseAsync))
                .LogError(ex, "Sending prompt ({}) ... Failed: {message} (elapsed time: {}ms, request time: {}ms)", callerCoordinates, ex.Message, wallClock.ElapsedMilliseconds, requestClock.ElapsedMilliseconds);

            return ex.Message;
        }

        // Deliberately omit catch (Exception ex) to let UnhandledExceptionHandler log unexpected exceptions.
    }
}