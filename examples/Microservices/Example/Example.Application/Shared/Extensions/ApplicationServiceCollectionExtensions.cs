using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Application.Shared.Extensions;

public static class ApplicationServiceCollectionExtension
{
    public static ApplicationServiceCollectionExtensions ApplicationLayer(
        this IServiceCollection services)
        => new ApplicationServiceCollectionExtensions(services);
}

public class ApplicationServiceCollectionExtensions
{
    private readonly IServiceCollection _services;
    public ApplicationServiceCollectionExtensions(
        IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollection AddMediatRServices()
    {
        _services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        return _services;
    }

    public IServiceCollection AddAutoMapperServices()
    {
        _services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ExampleMappingProfile>();
        }, Assembly.GetExecutingAssembly());
        return _services;
    }
}