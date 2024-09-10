namespace Cellm.Services.Telemetry.Sentry;

internal class SentryTelemetryConfiguration
{
    public bool IsEnabled { get; init; }

    public string Dsn { get; init; } = string.Empty;

    public bool Debug { get; init; }

    public float TracesSampleRate { get; init; }

    public float ProfilesSampleRate { get; init; }

    public string Environment { get; init; } = string.Empty;
}
