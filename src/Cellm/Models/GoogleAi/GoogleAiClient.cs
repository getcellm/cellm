using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.GoogleAi;

internal class GoogleAiClient : IClient
{
    private readonly GoogleAiConfiguration _googleAiConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;
    private readonly ISerde _serde;

    public GoogleAiClient(
        IOptions<GoogleAiConfiguration> googleAiConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache,
        ISerde serde)
    {
        _googleAiConfiguration = googleAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
        _serde = serde;
    }

    public async Task<Prompt> Send(Prompt prompt, string? provider, string? model, Uri? baseAddress)
    {
        var transaction = SentrySdk.StartTransaction(typeof(GoogleAiClient).Name, nameof(Send));
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

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

        if (_cache.TryGetValue(requestBody, out object? value) && value is Prompt assistantPrompt)
        {
            return assistantPrompt;
        }

        var json = _serde.Serialize(requestBody);
        var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

        string path = $"/v1beta/models/{model ?? _googleAiConfiguration.DefaultModel}:generateContent?key={_googleAiConfiguration.ApiKey}";
        var address = baseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(baseAddress, path);

        var response = await _httpClient.PostAsync(address, jsonAsString);
        var responseBodyAsString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = _serde.Deserialize<ResponseBody>(responseBodyAsString);
        var assistantMessage = responseBody?.Candidates?.SingleOrDefault()?.Content?.Parts?.SingleOrDefault()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        assistantPrompt = new PromptBuilder(prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        _cache.Set(requestBody, assistantPrompt);

        var tags = new Dictionary<string, string> {
            { nameof(provider), provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
            { nameof(model), model?.ToLower() ?? _googleAiConfiguration.DefaultModel },
            { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
        };

        var inputTokens = responseBody?.UsageMetadata?.PromptTokenCount ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var outputTokens = responseBody?.UsageMetadata?.CandidatesTokenCount ?? -1;
        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        transaction.Finish();

        return assistantPrompt;
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
