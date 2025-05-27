using System.Text;
using Cellm.AddIn.Exceptions;
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
    [ExcelFunction(Name = "PROMPT", Description = "Send a prompt to the default model")]
    public static object Prompt(
    [ExcelArgument(AllowReference = true, Name = "InstructionsOrContext", Description = "A string with instructions or a cell or range of cells with context")] object context,
    [ExcelArgument(AllowReference = true, Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
    [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        var configuration = CellmAddIn.Services.GetRequiredService<IConfiguration>();

        var provider = configuration.GetSection(nameof(ProviderConfiguration)).GetValue<string>(nameof(ProviderConfiguration.DefaultProvider))
            ?? throw new ArgumentException(nameof(ProviderConfiguration.DefaultProvider));

        var model = configuration.GetSection($"{provider}Configuration").GetValue<string>(nameof(IProviderConfiguration.DefaultModel))
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
    /// <param name="instructionsOrContext">A string with instructions or a cell or range of cells with context.</param>
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
    [ExcelFunction(Name = "PROMPTWITH", Description = "Send a prompt to a specific model")]
    public static object PromptWith(
        [ExcelArgument(AllowReference = true, Name = "Provider/Model")] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = "InstructionsOrContext", Description = "A string with instructions or a cell or range of cells with context")] object instructionsOrContext,
        [ExcelArgument(AllowReference = true, Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var argumentParser = CellmAddIn.Services.GetRequiredService<ArgumentParser>();
            var providerConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ProviderConfiguration>>();

            // We must parse arguments on the main thread
            var arguments = argumentParser
                .AddProvider(providerAndModel)
                .AddModel(providerAndModel)
                .AddInstructionsOrCells(instructionsOrContext)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .Parse();

            // ObserveResponse will send request on another thread and update the cell value on the main thread
            return ExcelAsyncUtil.Observe(
                nameof(PromptWith),
                new object[] { providerAndModel, instructionsOrContext, instructionsOrTemperature, temperature },
                () => new ObserveResponse(arguments));
        }
        catch (GettingDataException)
        {
            return ExcelError.ExcelErrorGettingData;
        }
        catch (CellmException ex)
        {
            SentrySdk.CaptureException(ex);

            var logger = CellmAddIn.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(PromptWith));

            logger.LogError(ex, "{method} failed", nameof(PromptWith));

            return ExcelError.ExcelErrorValue;
        }
    }

    private static bool IsCellGettingData(object argument)
    {
        return argument switch
        {
            ExcelError.ExcelErrorGettingData => true,
            object[,] cells => cells.Cast<object>().Any(cell => cell is ExcelError.ExcelErrorGettingData),
            _ => false
        };
    }
}