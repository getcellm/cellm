namespace Cellm.AddIn.Prompts;

internal static class CellmPrompts
{
    public const string SystemMessage = @"
<input>
The user has called you via an Excel formula. 
The Excel sheet is rendered as a table where each cell conist of its coordinate and value.
The table is your context and you must use it when following the user's instructions.
<input>

<constraints>
If you cannot find any instructions, or you cannot follow user's instructions in a cell-appropriate format, reply with ""#INSTRUCTION_ERROR?"" and nothing else.
</constraints>

<output>
Return ONLY the result of following the user's instructions as plain text without formatting.
Your response MUST be EITHER:

- A single word or number
- A comma-separated list of words or numbers
- A sentence

Do not provide explanations, steps, or engage in conversation.
</output>
";

    public const string InlineInstructions = "Analyze the context carefully and follow any instructions within the table.";

}
