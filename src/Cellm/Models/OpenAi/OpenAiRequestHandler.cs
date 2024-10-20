using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Cellm.Tools;
using Microsoft.Extensions.Options;

namespace Cellm.Models.OpenAi;

internal class OpenAiRequestHandler : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
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
        const string path = "/v1/chat/completions";
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

    public string Serialize(OpenAiRequest request)
    {
        var openAiPrompt = new PromptBuilder(request.Prompt)
            .AddSystemMessage()
            .Build();

        var chatCompletionRequest = new OpenAiChatCompletionRequest
        {
            Model = openAiPrompt.Model,
            Messages = openAiPrompt.ToOpenAiMessages(),
            MaxTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = openAiPrompt.Temperature,
            Tools = _tools.ToOpenAiTools(),
            ToolChoice = "auto"
        };

        return _serde.Serialize(chatCompletionRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public OpenAiResponse Deserialize(OpenAiRequest request, string responseBodyAsString)
    {
        var responseBody = _serde.Deserialize<OpenAiChatCompletionResponse>(responseBodyAsString);

        var tags = new Dictionary<string, string> {
            { nameof(request.Provider), request.Provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
            { nameof(request.Prompt.Model), request.Prompt.Model ?.ToLower() ?? _openAiConfiguration.DefaultModel },
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

        var choice = responseBody?.Choices?.FirstOrDefault() ?? throw new CellmException("Empty response from OpenAI API");

        var toolCalls = choice.Message.ToolCalls?
            .Select(x => new ToolCall(
                Id: x.Id,
                Name: x.Function.Name,
                Arguments: x.Function.Arguments,
                Result: null))
            .ToList();

        var message = new Message(choice.Message.Content, Roles.Assistant, toolCalls);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(message)
            .Build();

        return new OpenAiResponse(prompt);
    }
}
