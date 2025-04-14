using System.Reflection;
using Example.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Application.Shared.Extensions;

public static class ApplicationServiceCollectionExtension
{
    public static ApplicationServiceCollectionExtensions ApplicationServices(
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
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        return _services;
    }
    /*
    public IServiceCollection AddAutoMapperServices()
    {
        _services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ExampleMappingProfile>();
        }, Assembly.GetExecutingAssembly());
        return _services;
    }*/
}