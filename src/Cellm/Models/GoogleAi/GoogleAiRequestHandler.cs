using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.GoogleAi;

internal class GoogleAiRequestHandler : IModelRequestHandler<GoogleAiRequest, GoogleAiResponse>
{
    private readonly GoogleAiConfiguration _googleAiConfiguration;
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ISerde _serde;

    public GoogleAiRequestHandler(
        IOptions<GoogleAiConfiguration> googleAiConfiguration,
        IOptions<CellmConfiguration> cellmConfiguration,
        HttpClient httpClient,
        ISerde serde)
    {
        _googleAiConfiguration = googleAiConfiguration.Value;
        _cellmConfiguration = cellmConfiguration.Value;
        _httpClient = httpClient;
        _serde = serde;
    }

    public async Task<GoogleAiResponse> Handle(GoogleAiRequest request, CancellationToken cancellationToken)
    {
        string path = $"/v1beta/models/{request.Prompt.Model ?? _googleAiConfiguration.DefaultModel}:generateContent?key={_googleAiConfiguration.ApiKey}";
        var address = request.BaseAddress is null ? new Uri(path, UriKind.Relative) : new Uri(request.BaseAddress, path);

        var json = Serialize(request);
        var jsonAsStringContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(address, jsonAsStringContent, cancellationToken);
        var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{nameof(GoogleAiRequest)} failed: {responseBodyAsString}", null, response.StatusCode);
        }

        return Deserialize(request, responseBodyAsString);
    }

    public string Serialize(GoogleAiRequest request)
    {
        var requestBody = new GoogleAiRequestBody
        {
            SystemInstruction = new GoogleAiContent
            {
                Parts = new List<GoogleAiPart> { new GoogleAiPart { Text = request.Prompt.SystemMessage } }
            },
            Contents = new List<GoogleAiContent>
            {
                new GoogleAiContent
                {
                    Parts = request.Prompt.Messages.Select(x => new GoogleAiPart { Text = x.Content }).ToList()
                }
            }
        };

        return _serde.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public GoogleAiResponse Deserialize(GoogleAiRequest request, string response)
    {
        var responseBody = _serde.Deserialize<GoogleAiResponseBody>(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var tags = new Dictionary<string, string> {
            { nameof(request.Provider), request.Provider?.ToLower() ?? _cellmConfiguration.DefaultProvider },
            { nameof(request.Prompt.Model), request.Prompt.Model?.ToLower() ?? _googleAiConfiguration.DefaultModel },
            { nameof(_httpClient.BaseAddress), _httpClient.BaseAddress?.ToString() ?? string.Empty }
        };

        var inputTokens = responseBody?.UsageMetadata?.PromptTokenCount ?? -1;
        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var outputTokens = responseBody?.UsageMetadata?.CandidatesTokenCount ?? -1;
        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        var assistantMessage = responseBody?.Candidates?.SingleOrDefault()?.Content?.Parts?.SingleOrDefault()?.Text ?? throw new CellmException("#EMPTY_RESPONSE?");

        var assistantPrompt = new PromptBuilder(request.Prompt)
            .AddAssistantMessage(assistantMessage)
            .Build();

        return new GoogleAiResponse(assistantPrompt);
    }
}
