﻿using System.Diagnostics;
using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public static class Functions
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
    [ExcelArgument(Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
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
        [ExcelArgument(Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var argumentParser = CellmAddIn.Services.GetRequiredService<ArgumentParser>();
            var providerConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<ProviderConfiguration>>();

            var arguments = argumentParser
                .AddProvider(providerAndModel)
                .AddModel(providerAndModel)
                .AddInstructionsOrContext(instructionsOrContext)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .Parse();

            var userMessage = new StringBuilder()
                .AppendLine(arguments.Instructions)
                .AppendLine(arguments.Context)
                .ToString();

            var prompt = new PromptBuilder()
                .SetModel(arguments.Model)
                .SetTemperature(arguments.Temperature)
                .SetMaxOutputTokens(providerConfiguration.CurrentValue.MaxOutputTokens)
                .AddSystemMessage(SystemMessages.SystemMessage)
                .AddUserMessage(userMessage)
                .Build();

            // ExcelAsyncUtil.Run yields Excel's UI thread, Task.Run enables async/await in inner code
            return ExcelAsyncUtil.Run(nameof(PromptWith), new object[] { providerAndModel, instructionsOrContext, instructionsOrTemperature, temperature }, () =>
            {
                return Task.Run(async () => await GetResponseAsync(prompt, arguments.Provider)).GetAwaiter().GetResult();
            });

        }
        catch (CellmException e)
        {
            SentrySdk.CaptureException(e);
            Debug.WriteLine(e);
            return e.Message;
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

    internal static async Task<string> GetResponseAsync(Prompt prompt, Provider provider)
    {
        var client = CellmAddIn.Services.GetRequiredService<Client>();
        var response = await client.GetResponseAsync(prompt, provider, CancellationToken.None);
        return response.Messages.Last().Text ?? throw new NullReferenceException("No text response");
    }
}
