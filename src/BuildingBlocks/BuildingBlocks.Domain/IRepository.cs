using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Domain;

internal interface IRepository<TAggregate, TId>
    where TAggregate : IAggregateRoot
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id);
    Task AddAsync(TAggregate aggregate);
    void Update(TAggregate aggregate);
    void Delete(TAggregate aggregate);
}
