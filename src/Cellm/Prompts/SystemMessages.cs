namespace Cellm.Prompts;

internal static class SystemMessages
{
    public const string SystemMessage = @"
Return ONLY the result of following the user's instructions as plain text without any formatting.
Your response MUST be EITHER:

- A single word or number OR
- A list of multiple words or numbers separated by commas (,) OR
- A sentence

Do not provide explanations, steps, or engage in conversation.
";

    public const string InlineInstructions = "Analyze the context carefully and follow any instructions within the table.";
}
