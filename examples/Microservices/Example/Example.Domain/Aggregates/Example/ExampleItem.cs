using Example.Domain.Common.Exceptions;
using Example.Domain.Common.Abstractions;

namespace Example.Domain.Aggregates.Example;

public class ExampleItem : Entity
{
    public string Description { get; private set; }

    public ExampleItem(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");
        Description = description;
    }
}