using System.Text;
using System.Text.Json;
using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

public class Cellm
{
    private static readonly string SystemMessage = @"
<input>
The user has called you via the ""Prompt"" Excel function in a cell formula. The argument to the formula is the range of cells the user selected, e.g. ""=Prompt(A1)"" or ""=Prompt(A1:D10)"" 
Multiple cells are rendered as a table where each cell is prepended with the its coordinates.
<input>

<constraints>
You can only solve tasks that return data suitable for a single cell in a spreadsheet and in a format that is plain text or a numeric value.
If you cannot find any instructions, or you cannot follow user's instructions in a cell-appropriate format, reply with ""#INSTRUCTION_ERROR?"" and nothing else.
</constraints>

<output>
Return ONLY the result of following the user's instructions.
The result must be one of the following:

- A single word or number
- A comma-separated list of words or numbers
- A brief sentence

Be concise. Remember that cells have limited visible space.
Do not provide explanations, steps, or engage in conversation.
Ensure the output is directly usable in a spreadsheet cell.
</output>
";

    [ExcelFunction(Name = "PROMPT", Description = "Call a model with a prompt")]
    public static string Call(
        [ExcelArgument(AllowReference = true, Name = "Cells", Description = "A cell or range of cells")] object cells,
        [ExcelArgument(Name = "InstructionsOrTemperature", Description = "Model instructions or temperature")] object instructionsOrTemperature,
        [ExcelArgument(Name = "Temperature", Description = "Temperature")] object temperature)
    {
        try
        {
            var arguments = new ArgumentParser()
                .AddCells(cells)
                .AddInstructionsOrTemperature(instructionsOrTemperature)
                .AddTemperature(temperature)
                .Parse();

            var userMessage = new StringBuilder()
                .AppendLine(arguments.Instructions)
                .AppendLine(arguments.Cells)
                .ToString();

            var prompt = new PromptBuilder()
                .SetSystemMessage(SystemMessage)
                .SetTemperature(arguments.Temperature)
                .AddUserMessage(userMessage)
                .Build();

            return CallModelSync(prompt);
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
            var cellmConfiguration = ServiceLocator.ServiceProvider.GetRequiredService<IOptions<CellmConfiguration>>().Value;
            var clientFactory = ServiceLocator.ServiceProvider.GetRequiredService<IClientFactory>();
            var client = clientFactory.GetClient(cellmConfiguration.DefaultModelProvider);

            return client.Send(prompt);
        }
        catch (HttpRequestException ex)
        {
            throw new CellmException("API request failed", ex);
        }
        catch (JsonException ex)
        {
            throw new CellmException("Failed to deserialize API response", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new CellmException("Serialization or deserialization of request failed", ex);
        }
        catch (NullReferenceException ex)
        {
            throw new CellmException("Null reference encountered while processing the response", ex);
        }
        catch (Exception ex)
        {
            throw new CellmException("An unexpected error occurred", ex);
        }
    }
}