using Example.Domain.Aggregates.Example;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Application.Common.Events.DomainEventHandlers;

public class ExampleItemAddedDomainEventHandler
{
    public Task Handle(ExampleItemAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Implement side effects here. For example, log the event,
        // update analytics, or trigger an integration event.
        Console.WriteLine($"[Event Handler] Example item with ID {notification.ExampleItemId} added to aggregate {notification.ExampleId} at {notification.OccurredOn}");
        return Task.CompletedTask;
    }
}
