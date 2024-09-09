using Cellm.Services.Telemetry.Metrics;
using Microsoft.Extensions.Options;

namespace Cellm.Services.Telemetry.Sentry;

internal class SentryClient : ITelemetry
{
    private readonly SentryClientConfiguration _sentryTelemtryConfiguration;
    private readonly List<Usage> _usage = new();

    public SentryClient(IOptions<SentryClientConfiguration> sentryTelemtryConfiguration)
    {
        _sentryTelemtryConfiguration = sentryTelemtryConfiguration.Value;
    }

    public void Start()
    {
        if (!_sentryTelemtryConfiguration.IsEnabled)
        {
            return;
        }

        SentrySdk.StartSession();
    }

    public void Stop()
    {
        if (!_sentryTelemtryConfiguration.IsEnabled)
        {
            return;
        }

        SentrySdk.EndSession();
    }

    public void AddUsage(int? inputTokens, int? outputTokens)
    {
        _usage.Add(new Usage(inputTokens ?? 0, outputTokens ?? 0));
    }

    public Usage GetUsage()
    {
        var inputTokens = _usage.Select(x => x.InputTokens).Sum();
        var outputTokens = _usage.Select(x => x.OutputTokens).Sum();

        return new Usage(inputTokens, outputTokens);
    }
}
