using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Example.Api.Common.Extensions;

public static class ServiceCollectionExtension
{
    public static PresentationServiceCollectionExtensions ApiServices(
        this IServiceCollection services)
        => new PresentationServiceCollectionExtensions(services);
}

public class ServiceCollectionExtensions
{
    private readonly IServiceCollection _services;
    public ServiceCollectionExtensions(IServiceCollection services)
    {
        _services = services;
    }
    
    public IServiceCollection AddApplicationInsightsTelemetry(string? instrumentationKey)
    {
        return _services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = instrumentationKey
                ?? throw new ArgumentNullException(nameof(instrumentationKey),
                "Application Insights connection string is required.");
        });
    }

    public IServiceCollection AddMicrosoftIdentityWebApi(IConfigurationSection configurationSection)
    {
        _services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configurationSection);
        return _services;
    }

    public IServiceCollection AddControllers()
    {
        _services.AddControllers();
        return _services;
    }

    public IServiceCollection AddSwagger()
    {
        _services.AddEndpointsApiExplorer();
        _services.AddSwaggerGen();
        return _services;
    }
}