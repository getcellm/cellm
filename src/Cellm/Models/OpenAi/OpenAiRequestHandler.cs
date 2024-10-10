using System.ComponentModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Cellm.Tools;
using MediatR;
using Microsoft.Extensions.Options;
using static Cellm.Models.GoogleAi.GoogleAiClient;

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
        var json = Serialize(request);
        var jsonAsStringContent = new StringContent(json, Encoding.UTF8, "application/json");

        const string path = "/v1/chat/completions";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        var response = await _httpClient.PostAsync(address, jsonAsStringContent, cancellationToken);
        var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseBodyAsString, null, response.StatusCode);
        }

        var assistantMessage = Deserialize(responseBodyAsString);
        
        return new OpenAiResponse(new PromptBuilder(request.Prompt)
            .AddMessage(assistantMessage)
            .Build());
    }

    public string Serialize(OpenAiRequest request)
    {
        var requestBody = new RequestBody
        {
            Model = request.Prompt.Model,
            Messages = request.Prompt.ToOpenAiMessages(),
            MaxCompletionTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Temperature,
            Tools = _tools.ToOpenAiTools()
        };

        return _serde.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public Prompts.Message Deserialize(string response)
    {
        var responseBody = _serde.Deserialize<ResponseBody>(response);
        var choice = responseBody?.Choices?.FirstOrDefault() ?? throw new CellmException("#EMPTY_RESPONSE?");

        var toolRequests = choice.Message?.ToolCalls?
            .Select(x => new ToolRequest(
                Id: x.Id ?? throw new NullReferenceException(nameof(x.Id)),
                Name: x.Function?.Name ?? throw new NullReferenceException(nameof(x.Function.Name)),
                Arguments: _serde.Deserialize<Dictionary<string, string>>(x.Function?.Arguments ?? throw new NullReferenceException(nameof(x.Function.Arguments))),
                Response: null))
            .ToList();

        var content = choice.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");
        return new Prompts.Message(content, Roles.Assistant, toolRequests);
    }
}