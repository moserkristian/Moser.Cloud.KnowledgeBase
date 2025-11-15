using System;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
