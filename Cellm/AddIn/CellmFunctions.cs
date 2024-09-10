using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Cellm.Services;
using Cellm.Services.ModelProviders;
using ExcelDna.Integration;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn;

public static class CellmFunctions
{
    [ExcelFunction(Name = "PROMPT", Description = "Call a model with a prompt")]
    public static object Prompt(
    [ExcelArgument(AllowReference = true, Name = "Context", Description = "A cell or range of cells")] object context,
    [ExcelArgument(Name = "InstructionsOrTemperature", Description = "A cell or range of cells with instructions or a temperature")] object instructionsOrTemperature,
    [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var arguments = ServiceLocator.Get<ArgumentParser>()
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
                return Task.Run(async () => await PromptAsync(prompt)).GetAwaiter().GetResult();
            });
        }
        catch (CellmException ex)
        {
            SentrySdk.CaptureException(ex);
            return ex.Message;
        }
    }

    private static async Task<string> PromptAsync(Prompt prompt)
    {
        try
        {
            var client = ServiceLocator.Get<IClient>();
            var response = await client.Send(prompt);
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
