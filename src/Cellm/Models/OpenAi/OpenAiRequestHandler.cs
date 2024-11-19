using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.OpenAi.Models;
using Cellm.Prompts;
using Cellm.Tools;
using Microsoft.Extensions.Options;

namespace Cellm.Models.OpenAi;

internal class OpenAiRequestHandler : IModelRequestHandler<OpenAiRequest, OpenAiResponse>
{
    private readonly OpenAiConfiguration _openAiConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ToolRunner _toolRunner;
    private readonly Serde _serde;

    public OpenAiRequestHandler(
        IOptions<OpenAiConfiguration> openAiConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ToolRunner toolRunner,
        Serde serde)
    {
        _openAiConfiguration = openAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _toolRunner = toolRunner;
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
            throw new HttpRequestException($"{nameof(OpenAiRequest)} failed: {responseBodyAsString}", null, response.StatusCode);
        }

        return Deserialize(request, responseBodyAsString);
    }

    public string Serialize(OpenAiRequest request)
    {
        var openAiPrompt = new PromptBuilder(request.Prompt)
            .AddSystemMessage()
            .Build();

        var chatCompletionRequest = new OpenAiChatCompletionRequest(
            openAiPrompt.Model,
            openAiPrompt.ToOpenAiMessages(),
            _cellmConfiguration.MaxOutputTokens,
            openAiPrompt.Temperature,
            _toolRunner.ToOpenAiTools(),
            "auto");

        return _serde.Serialize(chatCompletionRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public OpenAiResponse Deserialize(OpenAiRequest request, string responseBodyAsString)
    {
        var responseBody = _serde.Deserialize<OpenAiChatCompletionResponse>(responseBodyAsString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var choice = responseBody?.Choices?.FirstOrDefault() ?? throw new CellmException("Empty response from OpenAI API");
        var toolCalls = choice.Message.ToolCalls?
            .Select(x => new ToolCall(x.Id, x.Function.Name, x.Function.Arguments, null))
            .ToList();

        var content = choice.Message.Content;
        var message = new Message(content, Roles.Assistant, toolCalls);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(message)
            .Build();

        return new OpenAiResponse(prompt);
    }
}
