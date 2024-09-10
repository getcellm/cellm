using Cellm.Services.Telemetry.Metrics;
using Microsoft.Extensions.Options;

namespace Cellm.Services.Telemetry.Sentry;

internal class SentryTelemetry : ITelemetry
{
    private readonly SentryTelemetryConfiguration _sentryTelemtryConfiguration;
    private readonly List<Usage> _usage = new();

    public SentryTelemetry(IOptions<SentryTelemetryConfiguration> sentryTelemtryConfiguration)
    {
        _sentryTelemtryConfiguration = sentryTelemtryConfiguration.Value;
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
