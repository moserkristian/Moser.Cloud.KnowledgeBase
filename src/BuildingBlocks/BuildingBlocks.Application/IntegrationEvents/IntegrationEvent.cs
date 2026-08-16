using Moser.Enterprise.Blueprint.BuildingBlocks.Application.Events;

using System;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Application.IntegrationEvents;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
