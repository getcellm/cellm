using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Anthropic;

internal class AnthropicRequestHandler : IModelRequestHandler<AnthropicRequest, AnthropicResponse>
{
    private readonly AnthropicConfiguration _anthropicConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;
    private readonly ISerde _serde;

    public AnthropicRequestHandler(
        IOptions<AnthropicConfiguration> anthropicConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache,
        ISerde serde)
    {
        _anthropicConfiguration = anthropicConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
        _serde = serde;
    }

    public async Task<AnthropicResponse> Handle(AnthropicRequest request, CancellationToken cancellationToken)
    {
        const string path = "/v1/messages";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        var json = Serialize(request);
        var jsonAsStringContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(address, jsonAsStringContent, cancellationToken);
        var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenAI API request failed: {responseBodyAsString}", null, response.StatusCode);
        }

        return Deserialize(request, responseBodyAsString);
    }

    public string Serialize(AnthropicRequest request)
    {
        var requestBody = new RequestBody
        {
            System = request.Prompt.SystemMessage,
            Messages = request.Prompt.Messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            Model = request.Prompt.Model ?? _anthropicConfiguration.DefaultModel,
            MaxTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Temperature
        };

        return _serde.Serialize(requestBody);
    }

    public AnthropicResponse Deserialize(AnthropicRequest request, string response)
    {
        var responseBody = _serde.Deserialize<ResponseBody>(response);

        var tags = new Dictionary<string, string> {
            { nameof(request.Provider), request.Provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
            { nameof(request.Prompt.Model), request.Prompt.Model ?.ToLower() ?? _anthropicConfiguration.DefaultModel },
            { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
        };

        var inputTokens = responseBody?.Usage?.InputTokens ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var outputTokens = responseBody?.Usage?.OutputTokens ?? -1;
        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var assistantMessage = responseBody?.Content?.Last()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        var prompt = new PromptBuilder(request.Prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        return new AnthropicResponse(prompt);
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
