using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Cellm.Tools;
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

        var openAiPrompt = new PromptBuilder(prompt)
            .AddSystemMessage()
            .Build();

        var requestBody = new RequestBody
        {
            Model = model ?? _openAiConfiguration.DefaultModel,
            Messages = openAiPrompt.Messages.Select(x => new Message { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            MaxTokens = _cellmConfiguration.MaxTokens,
            Temperature = prompt.Temperature,
            Tools = GetTools()
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
        var choice = responseBody?.Choices?.FirstOrDefault();

        var assistantMessage = choice?.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");

        if (choice?.FinishReason == "tool_calls")
        {
            var toolPromptBuilder = new PromptBuilder(prompt)
                .AddSystemMessage()
                .AddAssistantMessage(new Message());

        }        

        assistantPrompt = new PromptBuilder(prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        _cache.Set(requestBody, assistantPrompt);

        var tags = new Dictionary<string, string>
            {
                { nameof(provider), provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
                { nameof(model), model?.ToLower() ?? _openAiConfiguration.DefaultModel },
                { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
            };

        var inputTokens = responseBody?.Usage?.PromptTokens ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var outputTokens = responseBody?.Usage?.CompletionTokens ?? -1;
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

    private List<Tool> GetTools()
    {
        var attributes = typeof(Glob).GetMethod(nameof(Glob.Handle))?.GetCustomAttributes(typeof(DescriptionAttribute), false);
        var description = ((DescriptionAttribute)attributes![0]).Description;

        return new List<Tool>()
        {
            new() {
                Function = new Function {
                    Name = nameof(Glob),
                    Description = description,
                    Parameters = new Parameters
                    {
                        Type = "object",
                        Properties = new Dictionary<string, Property>
                        {
                            { 
                                "Path", new Property
                                {
                                    Description = "The root directory to start the glob search from"
                                }
                            },
                            { 
                                "IncludePatterns", new Property
                                {
                                    Description = "List of patterns to include in the search"
                                }
                            },
                            { 
                                "ExcludePatterns", new Property
                                {
                                    Description = "Optional list of patterns to exclude from the search"
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    class RequestBody
    {
        public string? Model { get; set; }

        public List<Message>? Messages { get; set; }

        [JsonPropertyName("max_completion_tokens")]
        public int MaxCompletionTokens { get; set; }

        public double Temperature { get; set; }

        public List<Tool>? Tools { get; set; }
    }

    class ResponseBody
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

    class Message
    {
        public string? Role { get; set; }

        public string? Content { get; set; }
    }

    class Choice
    {
        public int Index { get; set; }

        public Message? Message { get; set; }

        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<Tool>? ToolCalls;
    }

    class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }


    public class Tool
    {
        public string? Id;

        public string Type { get; set; } = "function";

        public Function? Function { get; set; }
    }

    public class Function
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public Parameters? Parameters { get; set; }

        public string? Arguments { get; set; }
    }

    public class Parameters
    {
        public string? Type { get; set; }

        public Dictionary<string, Property>? Properties { get; set; }

        public List<string>? Required { get; set; }

        public bool? AdditionalProperties { get; set; }
    }

    public class Property
    {
        public string? Type { get; set; }

        public string? Description { get; set; }
    }
}
