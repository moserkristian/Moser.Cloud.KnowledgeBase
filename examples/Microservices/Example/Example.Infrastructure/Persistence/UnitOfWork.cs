using Example.Application.Common.Interfaces;

namespace Example.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        return _context.ExecuteTransactionAsync(operation, cancellationToken);
    }
}