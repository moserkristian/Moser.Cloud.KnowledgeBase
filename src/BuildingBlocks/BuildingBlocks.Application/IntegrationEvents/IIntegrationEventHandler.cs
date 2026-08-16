using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Application.Events;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task Handle(TEvent @event, CancellationToken ct);
}
