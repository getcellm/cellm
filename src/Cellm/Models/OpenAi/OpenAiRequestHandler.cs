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
            Messages = request.Prompt.Messages(),
            MaxCompletionTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Temperature,
        };

        if (_openAiConfiguration.EnableTools)
        {
            requestBody.Tools = Convert();
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
        var choice = responseBody?.Choices?.FirstOrDefault() ?? throw new CellmException("Empty response");

        return new OpenAiResponse(new PromptBuilder(request.Prompt)
            .AddAssistantMessage(choice)
            .Build());
    }

    private List<OpenAiTool> ToOpenAiTools()
    {
        throw new NotImplementedException();
    }

    private List<Message> Convert(List<Prompts.Message> messages)
    {
        return messages
            .SelectMany(x => Convert(x))
            .ToList();
    }

    private IEnumerable<Message> Convert(Prompts.Message message)
    {
        return message.Role switch
        {
            Roles.Tool => message?.ToolRequests?
                .Select(x => new Message
                {
                    Content = x.Arguments.ToString() + " Result: " + x.Response,
                    Role = message.Role.ToString().ToLower(),
                    ToolCallId = x.Id
                }) ?? throw new CellmException(),
            _ => new List<Message>
            {
                new Message {
                    Content = message.Content,
                    Role = message.Role.ToString().ToLower(),
                    ToolCallId = null
                }
            },
        };
    }

    private Prompts.Message Convert(Choice choice)
    {
        var assistantContent = choice?.Message?.Content ?? throw new CellmException("#EMPTY_RESPONSE?");

        if (choice?.ToolCalls is not null && choice.ToolCalls.Any())
        {

        }

        return new Prompts.Message(assistantContent, Roles.Assistant, null);
    }

    private List<OpenAiTool> Convert()
    {
        var attributes = typeof(Glob).GetMethod(nameof(Glob.Handle))?.GetCustomAttributes(typeof(DescriptionAttribute), false);
        var description = ((DescriptionAttribute)attributes![0]).Description;

        return new List<OpenAiTool>()
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

};