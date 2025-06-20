using System.Collections.Concurrent;
using Cellm.Models.Behaviors;
using ExcelDna.Integration;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn.UserInterface.Ribbon;

internal class TokenUsageNotificationHandler(ILogger<TokenUsageNotificationHandler> logger) : INotificationHandler<TokenUsageNotification>
{
    private static readonly ConcurrentDictionary<string, long> _tokenUsage = new()
    {
        [nameof(UsageDetails.InputTokenCount)] = 0,
        [nameof(UsageDetails.OutputTokenCount)] = 0
    };

    private static readonly ConcurrentDictionary<DateTime, (long, double)> _tokensPerSecond = new();
    private readonly int _maxTokensPerSecondMeasurements = 100;

    public Task Handle(TokenUsageNotification notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            logger.LogWarning("Received null usage notification");
            return Task.CompletedTask;
        }

        _tokenUsage[nameof(UsageDetails.InputTokenCount)] += notification.Usage.InputTokenCount ?? 0;
        _tokenUsage[nameof(UsageDetails.OutputTokenCount)] += notification.Usage.OutputTokenCount ?? 0;

        _tokensPerSecond[DateTime.UtcNow] = (notification.Usage.OutputTokenCount ?? 0, notification.ElapsedTime.TotalSeconds);

        // Limit measurements to recent ones
        var cutoffTime = DateTime.UtcNow.AddSeconds(-30);
        var keysToRemove = _tokensPerSecond.Keys.Where(t => t < cutoffTime).ToList();

        foreach (var key in keysToRemove)
        {
            _tokensPerSecond.TryRemove(key, out _);
        }

        // Hard limit on number of measurements
        while (_tokensPerSecond.Count > _maxTokensPerSecondMeasurements)
        {
            var oldestMeasurement = _tokensPerSecond.Keys.Min();
            _tokensPerSecond.TryRemove(oldestMeasurement, out _);
        }

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            RibbonMain._ribbonUi?.InvalidateControl(nameof(RibbonMain.ModelGroupControlIds.TokenStatistics));
        });

        // Update speed statistics iff we have two measurements or more
        if (_tokensPerSecond.Count >= 2)
        {
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                RibbonMain._ribbonUi?.InvalidateControl(nameof(RibbonMain.ModelGroupControlIds.SpeedStatistics));
            });
        }

        return Task.CompletedTask;
    }

    public static long GetTotalInputTokens() => _tokenUsage[nameof(UsageDetails.InputTokenCount)];

    public static long GetTotalOutputTokens() => _tokenUsage[nameof(UsageDetails.OutputTokenCount)];

    public static double GetTokensPerSecond()
    {
        if (_tokensPerSecond.IsEmpty)
        {
            return 0;
        }

        return _tokensPerSecond.Sum(kvp => kvp.Value.Item1) / (_tokensPerSecond.Sum(kvp => kvp.Value.Item2) + 0.01);
    }

    public static double GetRequestsPerBusySecond()
    {
        if (_tokensPerSecond.IsEmpty)
        {
            return 0;
        }

        // Create a list of time intervals [StartTime, EndTime] for each request.
        var intervals = _tokensPerSecond
            .Select(kvp =>
            {
                var endTime = kvp.Key;
                var duration = kvp.Value.Item2;
                var startTime = endTime.AddSeconds(-duration);
                return (StartTime: startTime, EndTime: endTime);
            })
            .OrderBy(i => i.StartTime)
            .ToList();

        // Merge the overlapping intervals
        double busySeconds = 0;
        var (mergeIntervalStart, mergeIntervalEnd) = intervals[0];

        for (var i = 1; i < intervals.Count; i++)
        {
            var (startTime, endTime) = intervals[i];

            if (startTime < mergeIntervalEnd)
            {
                // The current interval overlaps with the merged one.
                // Extend the merged interval if the current one ends later.
                if (endTime > mergeIntervalEnd)
                {
                    mergeIntervalEnd = endTime;
                }
            }
            else
            {
                // A gap was found. The previous merged interval is complete.
                // Add its duration to the total and start a new merge interval.
                busySeconds += (mergeIntervalEnd - mergeIntervalStart).TotalSeconds;
                mergeIntervalStart = startTime;
                mergeIntervalEnd = endTime;
            }
        }

        // Add the duration of the last merged interval.
        busySeconds += (mergeIntervalEnd - mergeIntervalStart).TotalSeconds;

        if (busySeconds < 0.1)
        {
            return 1;
        }

        // Calculate RPS using busy time
        return _tokensPerSecond.Count / busySeconds;
    }
}
