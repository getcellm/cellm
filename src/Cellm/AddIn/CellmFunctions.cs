﻿using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Cellm.Models;
using Cellm.Services;
using ExcelDna.Integration;
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
    [ExcelArgument(AllowReference = true, Name = "Context", Description = "A cell or range of cells")] object context,
    [ExcelArgument(Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
    [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        var cellmConfiguration = ServiceLocator.Get<IOptions<CellmConfiguration>>().Value;

        return PromptWith(
            $"{cellmConfiguration.DefaultModelProvider}/{cellmConfiguration.DefaultModel}",
            context, 
            instructionsOrTemperature, 
            temperature);
    }

    /// <summary>
    /// Sends a prompt to a specific model.
    /// </summary>
    /// <param name="providerAndModel">The provider and model in the format "provider/model".</param>
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
    [ExcelFunction(Name = "PROMPTWITH", Description = "Send a prompt to a specific model")]
    public static object PromptWith(
        [ExcelArgument(AllowReference = true, Name = "Provider/Model")] object providerAndModel,
        [ExcelArgument(AllowReference = true, Name = "Context", Description = "A cell or range of cells")] object context,
        [ExcelArgument(Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var arguments = ServiceLocator.Get<ArgumentParser>()
                .AddProvider(providerAndModel)
                .AddModel(providerAndModel)
                .AddContext(context)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .Parse();

            var userMessage = new StringBuilder()
                .AppendLine(arguments.Instructions)
                .AppendLine(arguments.Context)
                .ToString();

            var prompt = new PromptBuilder()
                .SetSystemMessage(CellmPrompts.SystemMessage)
                .SetTemperature(arguments.Temperature)
                .AddUserMessage(userMessage)
                .Build();

            // ExcelAsyncUtil yields Excel's main thread, Task.Run enables async/await in inner code
            return ExcelAsyncUtil.Run(nameof(Prompt), new object[] { context, instructionsOrTemperature, temperature }, () =>
            {
                return Task.Run(async () => await CallModelAsync(prompt)).GetAwaiter().GetResult();
            });
        }
        catch (CellmException ex)
        {
            SentrySdk.CaptureException(ex);
            return ex.Message;
        }
    }

    /// <summary>
    /// Asynchronously sends a prompt to the specified model and retrieves the response.
    /// </summary>
    /// <param name="prompt">The prompt to send to the model.</param>
    /// <param name="provider">The provider of the model. If null, the default provider is used.</param>
    /// <param name="model">The specific model to use. If null, the default model is used.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model's response as a string.</returns>
    /// <exception cref="CellmException">Thrown when an unexpected error occurs during the operation.</exception>

    private static async Task<string> CallModelAsync(Prompt prompt, string? provider = null, string? model = null)
    {
        try
        {
            var client = ServiceLocator.Get<IClient>();
            var response = await client.Send(prompt, provider, model);
            return response.Messages.Last().Content;
        }
        catch (CellmException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CellmException("An unexpected error occurred", ex);
        }
    }
}