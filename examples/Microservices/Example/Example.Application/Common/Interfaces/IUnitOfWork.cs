namespace Example.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
