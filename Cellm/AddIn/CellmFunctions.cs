using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Prompts;
using ExcelDna.Integration;
using System.Text;

namespace Cellm.AddIn;

public static class CellmFunctions
{
    [ExcelFunction(Name = "PROMPT", Description = "Call a model with a prompt")]
    public static object CallModel(
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
                .SetSystemMessage(Messages.System)
                .SetTemperature(arguments.Temperature)
                .AddUserMessage(userMessage)
                .Build();

            return ExcelAsyncUtil.Run(nameof(CallModel), new object[] { context, instructionsOrTemperature, temperature }, () =>
            {
                return CallModelSync(prompt);
            });
        }
        catch (CellmException ex)
        {
            return ex.ToString();
        }
    }

    private static string CallModelSync(Prompt prompt)
    {
        try
        {
            var client = ServiceLocator.Get<IClient>();
            return client.Send(prompt);
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
