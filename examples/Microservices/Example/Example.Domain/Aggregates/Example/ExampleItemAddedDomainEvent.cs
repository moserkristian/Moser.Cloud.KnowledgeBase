using MediatR;

namespace Example.Domain.Aggregates.Example
{
    public class ExampleItemAddedDomainEvent : INotification
    {
        public Guid ExampleId { get; }
        public Guid ExampleItemId { get; }
        public DateTime OccurredOn { get; }

        public ExampleItemAddedDomainEvent(Guid exampleId, Guid exampleItemId)
        {
            ExampleId = exampleId;
            ExampleItemId = exampleItemId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
