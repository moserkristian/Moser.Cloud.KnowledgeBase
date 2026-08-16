using System;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Application.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
