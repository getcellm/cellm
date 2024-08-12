using System.Text;
using ExcelDna.Integration;

namespace Cellm;

public class Arguments
{
    public string Cells { get; }
    public string Instructions { get; } = @"
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
    public double Temperature { get; } = 0;

    public Arguments(object arg1, object arg2, object arg3)
    {

        var cells = new StringBuilder();
        var instructions = new StringBuilder();

        cells.AppendLine("<cells>");
        instructions.AppendLine("<instructions>");

        if (arg1 is ExcelReference arg1IsContext)
        {
            cells.Append(Table.Render(arg1IsContext));

            if (arg2 is not string)
            {
                instructions.AppendLine("Analyze the context carefully and follow any instructions within the table.");
            }
        }
        else
        {
            throw new CellmException("Error: Invalid first argument. Please provide a string, a range of cells.");
        }

        if (arg2 is string arg2IsStringInstruction)
        {
            instructions.AppendLine(arg2IsStringInstruction);
        }
        else if (arg2 is double arg2IsTemperature && 0 <= arg2IsTemperature && arg2IsTemperature <= 1)
        {
            Temperature = arg2IsTemperature;
        }
        else if (arg2 is ExcelMissing)
        {
            // No-op
        }
        else
        {
            throw new CellmException("Error: Invalid second argument. Please provide a string, a number in the range [0, 1], or omit second argument.");
        }

        if (arg3 is double arg3IsTemperature && 0 <= arg3IsTemperature && arg3IsTemperature <= 1 && arg2 is not double)
        {
            Temperature = arg3IsTemperature;
        }
        else if (arg3 is ExcelMissing)
        {
            // No-op
        }
        else
        {
            throw new CellmException("Error: Invalid third argument. Please provide a number in the range [0, 1] or omit third argument.");
        }

        cells.AppendLine("</cells>");
        instructions.AppendLine("</instructions>");

        Cells = cells.ToString();
        Instructions = instructions.ToString();
    }
}