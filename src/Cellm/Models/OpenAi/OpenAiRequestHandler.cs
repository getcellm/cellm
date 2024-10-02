using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Cellm.Tools;
using MediatR;
using Microsoft.Extensions.Options;

namespace Cellm.Models.OpenAi;

internal class OpenAiRequestHandler : IRequestHandler<OpenAiRequest, OpenAiResponse>
{
    private readonly OpenAiConfiguration _openAiConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ITools _tools;
    private readonly ISerde _serde;

    public OpenAiRequestHandler(
        IOptions<OpenAiConfiguration> openAiConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ITools tools,
        ISerde serde)
    {
        _openAiConfiguration = openAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _tools = tools;
        _serde = serde;
    }

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {

        var requestBody = new RequestBody
        {
            Model = request.Model ?? _openAiConfiguration.DefaultModel,
            Messages = Convert(request.Prompt),
            MaxCompletionTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Temperature,
        };

        if (_openAiConfiguration.EnableTools)
        {
            requestBody.Tools = Convert(_tools);
        }

        var json = _serde.Serialize(requestBody);
        var jsonAsString = new StringContent(json, Encoding.UTF8, "application/json");

        const string path = "/v1/chat/completions";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        var response = await _httpClient.PostAsync(address, jsonAsString, cancellationToken);
        var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var responseBody = _serde.Deserialize<ResponseBody>(responseBodyAsString);
        var choice = responseBody?.Choices?.FirstOrDefault();

        var assistantMessage = choice?.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");

        var assistantPrompt = new PromptBuilder(request.Prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        return new OpenAiResponse(assistantPrompt);
    }

    private List<Tool> ToOpenAiTools()
    {
        throw new NotImplementedException();
    }

    private List<Message> Convert(Prompt prompt)
    {
        var openAiPrompt = new PromptBuilder(prompt)
            .AddSystemMessage()
            .Build();

        return openAiPrompt
            .Messages
            .Select(x => Convert(x))
            .ToList();
    }

    private Message Convert(Prompts.Message message)
    {
        return message.Role switch
        {
            Roles.Tool => throw new NotImplementedException(),
            _ => new Message {
                Content = message.Content,
                Role = message.Role.ToString().ToLower(),
                ToolCallId = null
            },
        };
    }

    private Tool Convert(ToolRequest toolRequest)
    {
        if (string.IsNullOrEmpty(toolRequest.Response))
        {
            return new Tool();
        }

        return new Tool();
    }

    private Prompt Convert(List<Message> messages)
    {
        throw new NotImplementedException();
    }

    private List<Tool> Convert(ITools _)
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

        public string? ToolCallId { get; set; }
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
