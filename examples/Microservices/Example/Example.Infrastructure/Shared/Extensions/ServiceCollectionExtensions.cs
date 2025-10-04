using Example.Infrastructure.Persistence;
using Example.Application.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Infrastructure.Shared.Extensions;

public static class ServiceCollectionExtension
{
    public static InfrastructureServiceCollectionExtensions InfrastructureServices(
        this IServiceCollection services)
        => new InfrastructureServiceCollectionExtensions(services);
}

public class InfrastructureServiceCollectionExtensions
{
    private readonly IServiceCollection _services;
    public InfrastructureServiceCollectionExtensions(
        IServiceCollection services)
    {
        _services = services;
    }
    /*
    public IServiceCollection AddPersistenc(string connectionString)
    {
        _services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        return _services;
    }

    public IServiceCollection AddRepositories()
    {
        _services.AddScoped<IExampleRepository, ExampleRepository>();
        return _services;
    }

    public IServiceCollection AddUnitOfWork()
    {
        _services.AddScoped<IUnitOfWork, UnitOfWork>();
        return _services;
    }

    public IServiceCollection AddMessaging()
    {
        _services.AddTransient<IIntegrationEventService, IntegrationEventPublisher>();
        return _services;
    }*/
}