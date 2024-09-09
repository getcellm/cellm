using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Cellm.Services.Telemetry;
using Microsoft.Extensions.Options;

namespace Cellm.Services.ModelProviders.Anthropic;

internal class AnthropicClient : IClient
{
    private readonly AnthropicConfiguration _anthropicConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;
    private readonly ITelemetry _telemetry;

    public AnthropicClient(
        IOptions<AnthropicConfiguration> anthropicConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache,
        ITelemetry telemetry)
    {
        _anthropicConfiguration = anthropicConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
        _telemetry = telemetry;
    }

    public async Task<Prompt> Send(Prompt prompt)
    {
        var requestBody = new RequestBody
        {
            System = prompt.SystemMessage,
            Messages = prompt.Messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            Model = _anthropicConfiguration.DefaultModel,
            MaxTokens = _cellmConfiguration.MaxTokens,
            Temperature = prompt.Temperature
        };

        if (_cache.TryGetValue(requestBody, out object? value) && value is Prompt assistantPrompt)
        {
            return assistantPrompt;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(requestBody, options);
        var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/messages", jsonAsString);
        var responseBodyAsString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = JsonSerializer.Deserialize<ResponseBody>(responseBodyAsString, options);
        var assistantMessage = responseBody?.Content?.Last()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        _telemetry.AddUsage(responseBody?.Usage?.InputTokens, responseBody?.Usage?.OutputTokens);

        if (assistantMessage.StartsWith("#INSTRUCTION_ERROR?"))
        {
            throw new CellmException(assistantMessage);
        }

        var promptBuilder = new PromptBuilder();
        promptBuilder.SetPrompt(prompt);
        promptBuilder.AddAssistantMessage(assistantMessage);
        assistantPrompt = promptBuilder.Build();

        _cache.Set(requestBody, assistantPrompt);

        return assistantPrompt;
    }

    private class ResponseBody
    {
        public List<Content>? Content { get; set; }

        public string? Id { get; set; }

        public string? Model { get; set; }

        public string? Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string? StopSequence { get; set; }

        public string? Type { get; set; }

        public Usage? Usage { get; set; }
    }

    private class RequestBody
    {
        public List<Message>? Messages { get; set; }

        public string? System { get; set; }

        public string? Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        public double Temperature { get; set; }
    }

    private class Message
    {
        public string? Role { get; set; }

        public string? Content { get; set; }
    }

    private class Content
    {
        public string? Text { get; set; }

        public string? Type { get; set; }
    }

    private class Usage
    {
        public int InputTokens { get; set; }

        public int OutputTokens { get; set; }
    }
}
