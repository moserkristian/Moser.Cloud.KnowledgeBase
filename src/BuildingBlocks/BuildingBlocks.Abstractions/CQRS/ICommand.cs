namespace BuildingBlocks.Abstractions.CQRS;
public interface ICommand : IRequest
{
}

public interface ICommand<TResponse> : IRequest<TResponse>
{
}