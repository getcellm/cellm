using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.ModelProviders;

internal class GoogleGeminiClient : IClient
{
    private readonly GoogleGeminiConfiguration _googleGeminiConfiguration;
    private readonly CellmAddInConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;

    public GoogleGeminiClient(
        IOptions<GoogleGeminiConfiguration> googleGeminiConfiguration,
        IOptions<CellmAddInConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache)
    {
        _googleGeminiConfiguration = googleGeminiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
    }

    public string Send(Prompt prompt)
    {
        var requestBody = new RequestBody
        {
            SystemInstruction = new Content
            {
                Parts = new List<Part> { new Part { Text = prompt.SystemMessage } }
            },
            Contents = new List<Content>
            {
                new Content
                {
                    Parts = prompt.Messages.Select(x => new Part { Text = x.Content }).ToList()
                }
            }
        };

        if (_cache.TryGetValue(requestBody, out object? value) && value is string assistantMessage)
        {
            return assistantMessage;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(requestBody, options);
        var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

        var response = _httpClient.PostAsync($"/v1beta/models/{_googleGeminiConfiguration.DefaultModel}:generateContent?key={_googleGeminiConfiguration.ApiKey}", jsonAsString).Result;
        var responseBodyAsString = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = JsonSerializer.Deserialize<ResponseBody>(responseBodyAsString, options);
        assistantMessage = responseBody?.Candidates?.SingleOrDefault()?.Content?.Parts?.SingleOrDefault()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        if (assistantMessage.StartsWith("#INSTRUCTION_ERROR?"))
        {
            throw new CellmException(assistantMessage);
        }

        _cache.Set(requestBody, assistantMessage);

        return assistantMessage;
    }

    private class RequestBody
    {
        [JsonPropertyName("system_instruction")]
        public Content? SystemInstruction { get; set; }

        public List<Content>? Contents { get; set; }
    }

    public class ResponseBody
    {
        public List<Candidate>? Candidates { get; set; }

        public UsageMetadata? UsageMetadata { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }

        public string? FinishReason { get; set; }

        public int Index { get; set; }

        public List<SafetyRating>? SafetyRatings { get; set; }
    }

    public class Content
    {
        public List<Part>? Parts { get; set; }

        public string? Role { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }

    public class SafetyRating
    {
        public string? Category { get; set; }

        public string? Probability { get; set; }
    }

    public class UsageMetadata
    {
        public int PromptTokenCount { get; set; }

        public int CandidatesTokenCount { get; set; }

        public int TotalTokenCount { get; set; }
    }
}
