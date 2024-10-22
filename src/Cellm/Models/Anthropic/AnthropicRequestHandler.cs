using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models.Anthropic.Models;
using Cellm.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Anthropic;

internal class AnthropicRequestHandler : IModelRequestHandler<AnthropicRequest, AnthropicResponse>
{
    private readonly AnthropicConfiguration _anthropicConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ICache _cache;
    private readonly ISerde _serde;

    public AnthropicRequestHandler(
        IOptions<AnthropicConfiguration> anthropicConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ICache cache,
        ISerde serde)
    {
        _anthropicConfiguration = anthropicConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _cache = cache;
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
            System = request.Prompt.SystemMessage,
            Messages = request.Prompt.Messages.Select(x => new AnthropicMessage { Content = x.Content, Role = x.Role.ToString().ToLower() }).ToList(),
            Model = request.Prompt.Model ?? _anthropicConfiguration.DefaultModel,
            MaxTokens = _cellmConfiguration.MaxOutputTokens,
            Temperature = request.Prompt.Temperature
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

        var tags = new Dictionary<string, string> {
            { nameof(request.Provider), request.Provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
            { nameof(request.Prompt.Model), request.Prompt.Model ?.ToLower() ?? _anthropicConfiguration.DefaultModel },
            { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
        };

        var inputTokens = responseBody?.Usage?.InputTokens ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var outputTokens = responseBody?.Usage?.OutputTokens ?? -1;
        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var assistantMessage = responseBody?.Content?.Last()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        var prompt = new PromptBuilder(request.Prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        return new AnthropicResponse(prompt);
    }
}
