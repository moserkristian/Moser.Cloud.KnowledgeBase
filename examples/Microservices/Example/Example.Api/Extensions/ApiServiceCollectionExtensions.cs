using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Example.Api.Extensions;

public static class ApiServiceCollectionExtension
{
    public static ApiServiceCollectionExtensions ApplicationLayer(
        this IServiceCollection services)
        => new ApiServiceCollectionExtensions(services);
}

public class ApiServiceCollectionExtensions
{
    private readonly IServiceCollection _services;
    public ApiServiceCollectionExtensions(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollection AddAuthentication()
    {
        _services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
        return _services;
    }

    public IServiceCollection AddControllers()
    {
        _services.AddControllers();
        return _services;
    }

    public IServiceCollection AddSwaggerDocumentation()
    {
        _services.AddEndpointsApiExplorer();
        _services.AddSwaggerGen();
        return _services;
    }
}