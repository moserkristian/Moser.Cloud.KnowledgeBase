+ Root
    + src
        + Microservice_1
            + Application
                + Common
                    + Behaviors
                        + LoggingBehavior.cs
                        + PerformanceBehavior.cs
                        + TransactionBehavior.cs
                        + ValidationBehavior.cs
                + Shared
                    + Interfaces
                        + IUnitOfWork.cs
                        + IIntegrationEventService.cs
                + Features
                    + <AggregateRootName>s
                        + <FeatureName>
                            + <ChildEntityName>Dto/RequestDto/ResponseDto.cs
                            + <FeatureName>Command/Query.cs
                            + <FeatureName>Validator.cs
                            + <FeatureName>Handler.cs
                            + <FeatureName>Response.cs
                            + <FeatureName><FactName>IntegrationEvent.cs

## Application/Common/Behaviors/TransactionBehavior.cs

```csharp
[DebuggerStepThrough]
internal class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
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
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            TResponse response = await next();
            _logger.LogInformation("Transaction committed for {RequestType}", typeof(TRequest).Name);
            return response;
        }, cancellationToken);
    }
}
```

## Application/Shared/Interfaces/IIntegrationEventService.cs

```csharp
public interface IIntegrationEventService
{
    Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
```

## Application/Features/<AggregateRootName>s/<FeatureName>/<FeatureName>Command/Query.cs

```csharp
public sealed record <FeatureName>Command(
    Guid <AggregateRootName>Id,
    Guid <ChildEntityName>Id,
    IReadOnlyCollection<<ChildEntityName>Dto> <ChildEntityName>s)
    : IRequest<<FeatureName>Result>;
```
