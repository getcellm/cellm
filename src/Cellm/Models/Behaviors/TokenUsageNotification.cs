using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Behaviors;

public record TokenUsageNotification(
    UsageDetails Usage,
    Provider Provider,
    string? Model,
    TimeSpan ElapsedTime
) : INotification;