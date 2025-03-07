using Example.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Infrastructure.Shared.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenc(this IServiceCollection services, IConfiguration configuration)
    {
        // Register EF Core DbContext.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register repository implementations.
        services.AddScoped<IExampleAggregateRepository, ExampleAggregateRepository>();
        return services;
    }

    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        // Register messaging/integration services (e.g. Azure Service Bus publisher).
        services.AddTransient<IIntegrationEventService, IntegrationEventPublisher>();
        return services;
    }
}