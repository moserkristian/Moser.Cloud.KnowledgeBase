﻿using MediatR;
using FluentValidation;
using Example.Domain.Common.Exceptions;

namespace Example.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators, 
        ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var typeName = request.GetGenericTypeName();

        _logger.LogInformation("Validating command {CommandType}", typeName);

        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation errors - " +
                "{CommandType} - " +
                "Command: " +
                "{@Command} - " +
                "Errors: {@ValidationErrors}", 
                typeName, 
                request, 
                failures);

            throw new DomainException(
                $"Command Validation Errors for type " +
                $"{typeof(TRequest).Name}", 
                new ValidationException("Validation exception", failures));
        }

        return await next();
    }
}
