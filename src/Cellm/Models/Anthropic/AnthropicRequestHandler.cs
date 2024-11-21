using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Anthropic.Models;
using Cellm.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Anthropic;

internal class AnthropicRequestHandler : IModelRequestHandler<AnthropicRequest, AnthropicResponse>
{
    private readonly AnthropicConfiguration _anthropicConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly Serde _serde;

    public AnthropicRequestHandler(
        IOptions<AnthropicConfiguration> anthropicConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        Serde serde)
    {
        _anthropicConfiguration = anthropicConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
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
            throw new HttpRequestException($"{nameof(AnthropicRequest)} failed: {responseBodyAsString}", null, response.StatusCode);
        }

        return Deserialize(request, responseBodyAsString);
    }

    public string Serialize(AnthropicRequest request)
    {
        var requestBody = new AnthropicRequestBody
        {
            System = request.Prompt.Messages.Where(x => x.Role == ChatRole.System).First().Text,
            Messages = request.Prompt.Messages.Select(x => new AnthropicMessage { Content = x.Text, Role = x.Role.ToString().ToLower() }).ToList(),
            Model = request.Prompt.Options.ModelId ?? _anthropicConfiguration.DefaultModel,
            MaxTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Options.Temperature ?? _cellmConfiguration.DefaultTemperature,
        };

        return _serde.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public AnthropicResponse Deserialize(AnthropicRequest request, string response)
    {
        var responseBody = _serde.Deserialize<AnthropicResponseBody>(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var assistantMessage = responseBody?.Content?.Last()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        var prompt = new PromptBuilder(request.Prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        return new AnthropicResponse(prompt);
    }
}
