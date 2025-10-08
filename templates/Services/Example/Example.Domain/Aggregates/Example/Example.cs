using Example.Domain.Common.Exceptions;
using Example.Domain.Common.Abstractions;

namespace Example.Domain.Aggregates.Example;

public class Example : AggregateRoot
{
    public string Name { get; private set; }
    public List<ExampleItem> Items { get; private set; } = new List<ExampleItem>();

    public Example(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty.");
        Name = name;
    }

    public void AddItem(ExampleItem item)
    {
        if (item == null)
            throw new DomainException("Item cannot be null.");
        Items.Add(item);
        AddDomainEvent(new ExampleItemAddedDomainEvent(this.Id, item.Id));
    }
}
