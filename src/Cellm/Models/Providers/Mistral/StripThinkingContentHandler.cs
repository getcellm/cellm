using System.Text.Json.Nodes;

namespace Cellm.Models.Providers.Mistral;

/// Strips "thinking" content parts from Mistral Magistral responses before
/// the OpenAI SDK deserializes them. The OpenAI SDK does not recognize the
/// "thinking" content part type and throws ArgumentOutOfRangeException.
internal class StripThinkingContentHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!body.Contains("\"thinking\""))
        {
            return response;
        }

        var modified = StripThinkingContent(body);
        response.Content = new StringContent(modified, System.Text.Encoding.UTF8, "application/json");

        return response;
    }

    private static string StripThinkingContent(string json)
    {
        var node = JsonNode.Parse(json);

        if (node is null)
        {
            return json;
        }

        var choices = node["choices"]?.AsArray();

        if (choices is null)
        {
            return json;
        }

        foreach (var choice in choices)
        {
            var content = choice?["message"]?["content"];

            if (content is not JsonArray contentArray)
            {
                continue;
            }

            for (var i = contentArray.Count - 1; i >= 0; i--)
            {
                var type = contentArray[i]?["type"]?.GetValue<string>();

                if (type == "thinking")
                {
                    contentArray.RemoveAt(i);
                }
            }
        }

        return node.ToJsonString();
    }
}
