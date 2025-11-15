using Example.Domain.Common.Abstractions;

using System;

namespace Example.Domain.Aggregates.Example
{
    public class ExampleItemAddedDomainEvent : DomainEvent
    {
        public Guid ExampleId { get; }
        public Guid ExampleItemId { get; }
        public new DateTime OccurredOn { get; }

        public ExampleItemAddedDomainEvent(Guid exampleId, Guid exampleItemId)
        {
            ExampleId = exampleId;
            ExampleItemId = exampleItemId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
