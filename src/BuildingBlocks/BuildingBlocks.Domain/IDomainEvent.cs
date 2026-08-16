using System;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Domain;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
