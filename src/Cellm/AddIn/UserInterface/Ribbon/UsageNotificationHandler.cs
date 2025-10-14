using System.Collections.Concurrent;
using Cellm.Models.Behaviors;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn.UserInterface.Ribbon;

internal class UsageNotificationHandler(ILogger<UsageNotificationHandler> logger) : INotificationHandler<UsageNotification>
{
    private static readonly ConcurrentDictionary<string, long> _tokenUsage = new()
    {
        [nameof(UsageDetails.InputTokenCount)] = 0,
        [nameof(UsageDetails.OutputTokenCount)] = 0,
        [nameof(GetTotalPrompts)] = 0
    };

    private static readonly ConcurrentDictionary<DateTime, UsageNotification> _tokensPerSecond = new();
    private readonly int _maxTokensPerSecondMeasurements = 100;

    public Task Handle(UsageNotification notification, CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            logger.LogWarning("Received null usage notification");
            return Task.CompletedTask;
        }

        _tokenUsage[nameof(UsageDetails.InputTokenCount)] += notification.Usage.InputTokenCount ?? 0;
        _tokenUsage[nameof(UsageDetails.OutputTokenCount)] += notification.Usage.OutputTokenCount ?? 0;
        _tokenUsage[nameof(GetTotalPrompts)] += 1;

        _tokensPerSecond[notification.EndTime] = notification;

        var oldestMeasurement = _tokensPerSecond.Keys.DefaultIfEmpty(DateTime.UtcNow).Min();
        var window = DateTime.UtcNow.AddSeconds(-30);

        // Limit measurements to the most recent ones
        while (oldestMeasurement < window)
        {

            _tokensPerSecond.TryRemove(oldestMeasurement, out _);
            oldestMeasurement = _tokensPerSecond.Keys.DefaultIfEmpty(DateTime.UtcNow).Min();
        }

        // Hard limit on number of measurements
        while (_maxTokensPerSecondMeasurements < _tokensPerSecond.Count)
        {
            _tokensPerSecond.TryRemove(oldestMeasurement, out _);
            oldestMeasurement = _tokensPerSecond.Keys.DefaultIfEmpty(DateTime.UtcNow).Min();
        }

        RibbonMain.UpdateTokenStatistics(GetTotalInputTokens(), GetTotalOutputTokens(), GetTotalPrompts());
        RibbonMain.UpdateSpeedStatistics(GetTokensPerSecond(), GetRequestsPerBusySecond());

        return Task.CompletedTask;
    }

    private static long GetTotalInputTokens() => _tokenUsage[nameof(UsageDetails.InputTokenCount)];

    private static long GetTotalOutputTokens() => _tokenUsage[nameof(UsageDetails.OutputTokenCount)];

    private static long GetTotalPrompts() => _tokenUsage[nameof(GetTotalPrompts)];

    private static double GetTokensPerSecond()
    {
        if (_tokensPerSecond.IsEmpty)
        {
            return 0;
        }

        var totalDuration = _tokensPerSecond.Sum(kvp => (kvp.Value.EndTime - kvp.Value.StartTime).TotalSeconds);

        if (totalDuration < 0.01)
        {
            return 0;
        }

        var totalOutputTokens = _tokensPerSecond.Sum(kvp => kvp.Value.Usage.OutputTokenCount) ?? 0;

        return totalOutputTokens / totalDuration;
    }

    private static double GetRequestsPerBusySecond()
    {
        if (_tokensPerSecond.IsEmpty)
        {
            return 0;
        }

        // Create a list of time intervals [StartTime, EndTime] for each request.
        var intervals = _tokensPerSecond
            .Select(kvp => (kvp.Value.StartTime, kvp.Value.EndTime))
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
            return 0.1;
        }

        // Calculate RPS using busy time
        return _tokensPerSecond.Count / busySeconds;
    }
}
