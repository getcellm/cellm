using Cellm.Models.Providers;
using MediatR;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Behaviors;

public record UsageNotification(
    UsageDetails Usage,
    Provider Provider,
    string? Model,
    DateTime StartTime,
    DateTime EndTime
) : INotification;