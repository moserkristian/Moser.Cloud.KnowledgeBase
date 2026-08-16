using Microsoft.EntityFrameworkCore;

using Moser.Enterprise.Blueprint.BuildingBlocks.Infrastructure.Persistence;
using Moser.Enterprise.Blueprint.Catalog.Domain.AggregatesModel.ProductAggregate;

namespace Moser.Enterprise.Blueprint.Catalog.Infrastructure;

internal class CatalogDbContext : DbContextBase
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
