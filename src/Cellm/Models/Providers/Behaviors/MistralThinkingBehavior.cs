using System.Text.Json;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Behaviors;

/// <summary>
/// Extracts the answer if model produced a reasoning response.
/// </summary>
internal class MistralThinkingBehavior : IProviderBehavior
{
    public bool IsEnabled(Provider provider)
    {
        return provider == Provider.Mistral;
    }

    // No-op
    public void Before(Provider provider, Prompt prompt) { }

    public void After(Provider provider, Prompt prompt)
    {
        var assistantMessage = prompt.Messages.LastOrDefault();

        if (assistantMessage is null ||
            assistantMessage.Role != ChatRole.Assistant ||
            string.IsNullOrWhiteSpace(assistantMessage.Text))
        {
            return;
        }

        var assistantMessageText = assistantMessage.Text.Trim();

        // Quick check to see if string is array-like
        if (!assistantMessageText.StartsWith('[') || !assistantMessageText.EndsWith(']'))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(assistantMessageText);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            // Ignore thinking tokens and only return only the answer
            foreach (var element in doc.RootElement.EnumerateArray())
            {

                if (element.ValueKind == JsonValueKind.Object &&
                    element.TryGetProperty("type", out var type) &&
                    type.GetString() == "text" &&
                    element.TryGetProperty("text", out var text))
                {
                    assistantMessage.Contents = [new TextContent(text.GetString())];
                    return;
                }
            }
        }
        catch (JsonException)
        {
            // Wasn't a thinking response, ignore
        }
    }

    public uint Order => 30;
}
