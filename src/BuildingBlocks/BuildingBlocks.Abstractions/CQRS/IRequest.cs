namespace BuildingBlocks.Abstractions.CQRS;
public interface IRequest
{
    Guid RequestId { get; }
}

public interface IRequest<TResponse> : IRequest
{
}