namespace Moser.Enterprise.Blueprint.BuildingBlocks.Domain;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    protected AggregateRoot() { }
    protected AggregateRoot(TId id) => Id = id;
}
