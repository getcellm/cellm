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

        var assistantMessage = Deserialize(responseBodyAsString);
        
        return new OpenAiResponse(new PromptBuilder(request.Prompt)
            .AddMessage(assistantMessage)
            .Build());
    }

    private string Serialize(OpenAiRequest request)
    {
        var openAiPrompt = new PromptBuilder(request.Prompt)
            .AddSystemMessage()
            .Build();

        var chatCompletionRequest = new OpenAiChatCompletionRequest
        {
            Model = openAiPrompt.Model,
            Messages = openAiPrompt.Messages.Select(m => new OpenAiMessage
            {
                Role = m.Role.ToString().ToLower(),
                Content = m.Content,
                ToolCalls = m.ToolRequests?.Select(tr => new OpenAiToolCall
                {
                    Id = tr.Id,
                    Type = "function",
                    Function = new OpenAiFunctionCall
                    {
                        Name = tr.Name,
                        Arguments = _serde.Serialize(tr.Arguments)
                    }
                }).ToList()
            }).ToList(),
            MaxTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = openAiPrompt.Temperature,
            Tools = _tools.GetTools().Select(t => new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = new OpenAiParameters
                    {
                        Properties = t.Parameters.ToDictionary(
                            p => p.Key,
                            p => new OpenAiProperty
                            {
                                Type = p.Value.Type,
                                Description = p.Value.Description
                            }
                        ),
                        Required = new List<string>() // We don't have this information in the Tool class
                    }
                }
            }).ToList(),
            ToolChoice = "auto"
        };

        return _serde.Serialize(chatCompletionRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private Message Deserialize(string responseBodyAsString)
    {
        var response = _serde.Deserialize<OpenAiChatCompletionResponse>(responseBodyAsString);
        var choice = response.Choices.FirstOrDefault() ?? throw new CellmException("Empty response from OpenAI API");

        var toolRequests = choice.Message.ToolCalls?
            .Select(x => new ToolRequest(
                Id: x.Id,
                Name: x.Function.Name,
                Arguments: _serde.Deserialize<Dictionary<string, string>>(x.Function.Arguments),
                Response: null))
            .ToList();

        return new Message(choice.Message.Content, Roles.Assistant, toolRequests);
    }
}
