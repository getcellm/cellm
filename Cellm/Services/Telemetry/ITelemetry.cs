using Cellm.Services.Telemetry.Metrics;

namespace Cellm.Services.Telemetry;

internal interface ITelemetry
{
    public void Start();

    public void Stop();

    public void AddUsage(int? inputTokens, int? outputTokens);

    public Usage GetUsage();

};