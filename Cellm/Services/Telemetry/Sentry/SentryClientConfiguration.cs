namespace Cellm.Services.Telemetry.Sentry;

internal class SentryClientConfiguration
{
    public bool IsEnabled { get; init; }

    public string Dsn { get; init; } = string.Empty;

    public bool Debug { get; init; }

    public float TracesSampleRate { get; init; }

    public float ProfilesSampleRate { get; init; }
}
