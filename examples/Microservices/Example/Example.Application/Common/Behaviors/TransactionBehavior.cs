using MediatR;
using System.Diagnostics;
using Example.Application.Common.Persistence;

namespace Example.Application.Common.Behaviors;

[DebuggerStepThrough]
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        _logger.LogInformation("Starting transaction for {RequestType}", typeof(TRequest).Name);
        return await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            TResponse response = await next();
            _logger.LogInformation("Transaction committed for {RequestType}", typeof(TRequest).Name);
            return response;
        }, cancellationToken);
    }
}