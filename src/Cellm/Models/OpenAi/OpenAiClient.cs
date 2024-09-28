using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.OpenAi;

internal class OpenAiClient : IClient
{
    private readonly OpenAiConfiguration _openAiConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;
    private readonly ISerde _serde;

    public OpenAiClient(
        IOptions<OpenAiConfiguration> openAiConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache,
        ISerde serde)
    {
        _openAiConfiguration = openAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
        _serde = serde;
    }

    public async Task<Prompt> Send(Prompt prompt, string? provider, string? model, Uri? baseAddress)
    {
        var transaction = SentrySdk.StartTransaction(typeof(OpenAiClient).Name, nameof(Send));
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var openAiPrompt = new PromptBuilder()
            .SetSystemMessage(prompt.SystemMessage)
            .AddSystemMessage()
            .AddMessages(prompt.Messages)
            .SetTemperature(prompt.Temperature)
            .Build();

        var requestBody = new RequestBody
        {
            Model = model ?? _openAiConfiguration.DefaultModel,
            Messages = openAiPrompt.Messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            MaxTokens = _cellmConfiguration.MaxTokens,
            Temperature = prompt.Temperature
        };

        if (_cache.TryGetValue(requestBody, out object? value) && value is Prompt assistantPrompt)
        {
            return assistantPrompt;
        }

        var json = _serde.Serialize(requestBody);
        var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

        const string path = "/v1/chat/completions";
        var address = baseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(baseAddress, path);

        var response = await _httpClient.PostAsync(address, jsonAsString);
        var responseBodyAsString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = _serde.Deserialize<ResponseBody>(responseBodyAsString);
        var assistantMessage = responseBody?.Choices?.FirstOrDefault()?.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");

        if (assistantMessage.StartsWith("#INSTRUCTION_ERROR?"))
        {
            throw new CellmException(assistantMessage);
        }

        assistantPrompt = new PromptBuilder(prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        _cache.Set(requestBody, assistantPrompt);

        var inputTokens = responseBody?.Usage?.PromptTokens ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags: new Dictionary<string, string> {
                    { nameof(provider), provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
                    { nameof(model), model?.ToLower() ?? _openAiConfiguration.DefaultModel },
                    { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
                }
            );
        }

        var outputTokens = responseBody?.Usage?.CompletionTokens ?? -1;
        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags: new Dictionary<string, string>
                {
                    { nameof(provider), provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
                    { nameof(model), model?.ToLower() ?? _openAiConfiguration.DefaultModel },
                    { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
                }
            );
        }

        transaction.Finish();

        return assistantPrompt;
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
