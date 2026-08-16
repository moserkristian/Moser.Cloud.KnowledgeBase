using Moser.Enterprise.Blueprint.BuildingBlocks.Infrastructure.Persistence;

namespace Moser.Enterprise.Blueprint.Catalog.Infrastructure;

internal class UnitOfWork : UnitOfWorkBase<CatalogDbContext>
{
    public UnitOfWork(CatalogDbContext context) : base(context) { }
}
