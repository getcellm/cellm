using Cellm.AddIn.Exceptions;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using Cellm.Models.OpenAi;
using Cellm.AddIn;
using Microsoft.Office.Interop.Excel;
using static Cellm.Models.GoogleAi.GoogleAiClient;
using System.Net.Http;

namespace Cellm.Models;

internal abstract class ModelProvider : ISerde
{
    private readonly ICache _cache;
    private readonly ISerde _serde;

    private readonly JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public ModelProvider(
        ICache cache,
        ISerde serde) 
    {
        _cache = cache;
        _serde = serde;
    }

    public ITransactionTracer StartTransaction(string name, string operation)
    {
        var transaction = SentrySdk.StartTransaction(name, operation);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
        return transaction;
    }

    public void StopTransaction(ITransactionTracer transaction)
    {
        transaction.Finish();
    }

    public string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? _defaultOptions);
    }

    public TValue Deserialize<TValue>(string value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<TValue>(value, options ?? _defaultOptions) ?? throw new CellmException("Failed to deserialize responds");
    }

    public void LogUsage(int inputTokens, int outputTokens, string provider, string model, Uri baseAddress)
    {
        var tags = new Dictionary<string, string>
            {
                { nameof(provider), provider.ToLower() },
                { nameof(model), model.ToLower() },
                { nameof(baseAddress), baseAddress.ToString() }
            };

        if (inputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("InputTokens",
                inputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }

        if (outputTokens > 0)
        {
            SentrySdk.Metrics.Distribution("OutputTokens",
                outputTokens,
                unit: MeasurementUnit.Custom("token"),
                tags);
        }
    }
}
