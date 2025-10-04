using System.Reflection;
//using Example.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Application.Shared.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registrácia CQRS handlerov (napr. CommandHandler, QueryHandler)
        /*services..Scan(scan => scan
            .FromAssemblyOf<ApplicationServiceCollectionExtensions>()
            .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Registrácia validátorov (napr. pre vstupné modely)
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationServiceCollectionExtensions>()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        */
        // Registrácia ďalších služieb aplikácie
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}


/*
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
*/
/*
public IServiceCollection AddAutoMapperServices()
{
    _services.AddAutoMapper(cfg =>
    {
        cfg.AddProfile<ExampleMappingProfile>();
    }, Assembly.GetExecutingAssembly());
    return _services;
}*/

//}