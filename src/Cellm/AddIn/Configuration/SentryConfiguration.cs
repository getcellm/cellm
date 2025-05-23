namespace Cellm.AddIn.Configuration;

internal class SentryConfiguration
{
    public bool IsEnabled { get; init; } = true;

    public string Dsn { get; init; } = "https://b0e331659c961d98d679ca441a753498@o4507924647378944.ingest.de.sentry.io/4507924651901008";

    public float TracesSampleRate { get; init; } = 1F;

    public float ProfilesSampleRate { get; init; } = 0.1F;

    public string Environment { get; init; } = "Production";

    public bool Debug { get; init; } = false;
}
