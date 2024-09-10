using Cellm.Services.Telemetry.Metrics;

namespace Cellm.Services.Telemetry;

internal interface ITelemetry
{

    public void AddUsage(int? inputTokens, int? outputTokens);

    public Usage GetUsage();

};