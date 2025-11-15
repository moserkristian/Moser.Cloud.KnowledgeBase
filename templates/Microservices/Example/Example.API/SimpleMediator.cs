using Microsoft.Extensions.Logging;

using System.Threading;
using System.Threading.Tasks;

namespace Example.Api;

public sealed record ExampleRequest(string someInput);

public interface IRequestHandler<IRequest, TResponse>
{
    Task<TResponse> Handle(IRequest request, CancellationToken cancellationToken = default);
}

internal sealed class ExampleRequestHandler()
    : IRequestHandler<ExampleRequest, string>
{
    public async Task<string> Handle(ExampleRequest request, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(request.someInput);
    }
}



//Logging Behavior
internal sealed class LoggingRequestHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> innerHandler,
    ILogger<LoggingRequestHandler<TRequest, TResponse>> logger) : IRequestHandler<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
       logger.LogInformation("Begin pipeline behavior {Request}", request.GetType().Name);

        var response = await innerHandler.Handle(request, cancellationToken);

        logger.LogInformation("End pipeline behavior {Request}", request.GetType().Name);

        return response;
    }
}
