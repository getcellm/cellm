using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.Exceptions;
using Cellm.Prompts;
using Cellm.Services.Configuration;
using Microsoft.Extensions.Options;

namespace Cellm.ModelProviders;

internal class OpenAiClient : IClient
{
    private readonly OpenAiConfiguration _openAiConfiguration;
    private readonly CellmAddInConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;

    public OpenAiClient(
        IOptions<OpenAiConfiguration> openAiConfiguration,
        IOptions<CellmAddInConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache)
    {
        _openAiConfiguration = openAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<string> Send(Prompt prompt)
    {
        var requestBody = new RequestBody
        {
            Model = _openAiConfiguration.DefaultModel,
            Messages = prompt.Messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            MaxTokens = _cellmConfiguration.MaxTokens,
            Temperature = prompt.Temperature
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

        var response = await _httpClient.PostAsync("/v1/chat/completions", jsonAsString);
        var responseBodyAsString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = JsonSerializer.Deserialize<ResponseBody>(responseBodyAsString, options);
        assistantMessage = responseBody?.Choices?.FirstOrDefault()?.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");

        if (assistantMessage.StartsWith("#INSTRUCTION_ERROR?"))
        {
            throw new CellmException(assistantMessage);
        }

        _cache.Set(requestBody, assistantMessage);

        return assistantMessage;
    }

    private class RequestBody
    {
        public string? Model { get; set; }

        public List<Message>? Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        public double Temperature { get; set; }
    }

    private class ResponseBody
    {
        public string? Id { get; set; }

        public string? Object { get; set; }
        public long Created { get; set; }
        public string? Model { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        public List<Choice>? Choices { get; set; }

        public Usage? Usage { get; set; }
    }

    private class Message
    {
        public string? Role { get; set; }

        public string? Content { get; set; }
    }

    private class Choice
    {
        public int Index { get; set; }

        public Message? Message { get; set; }

        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
