using Example.Api.Common.Extensions;

namespace Example.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration)
            .AddPresentation(builder.Configuration); // REST, Swagger, Filters

        builder.Services
            .ApiServices().AddApplicationInsightsTelemetry(builder.Configuration.GetConnectionString("ApplicationInsights"))
            .ApiServices().AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
            .ApiServices().AddControllers()
            .ApiServices().AddSwagger();


        builder.Host.UseSerilog();
        builder.Services.AddApiServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);


        // OWN MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR
        builder.Services.AddScoped<ExampleRequestHandler>();
        builder.Services.AddScoped<IRequestHandler<ExampleRequest, string>>(sp =>
             new LoggingRequestHandler<ExampleRequest, string>(
                 sp.GetRequiredService<ExampleRequestHandler>(),
                 sp.GetRequiredService<ILogger<LoggingRequestHandler<ExampleRequest, string>>>()));
        // END OWN MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR

        var app = builder.Build();

        app.UseInfrastructure(); // ExceptionHandler, Migrations, etc.
        app.UsePresentation();    // Controllers, Auth, etc.

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Normal logger: Application has started successfully.");

        app.MapDefaultEndpoints();

        // OWN MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR
        app.MapGet("/examples/{exampleInput}", async(
            string exampleInput,
            IRequestHandler<ExampleRequest, string> handler,
            CancellationToken cancellationToken) =>
        {
            var request = new ExampleRequest(exampleInput);
            var response = await handler.Handle(request, cancellationToken);
            return response is not null ? Results.Ok(response) : Results.NotFound();
        })
            .WithName("GetExample")
            .WithTags("Examples")
            .WithOpenApi();
        // END OWN MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR MEDIATOR


        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }

    private static void InitializeBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
}


//TODO : ADD INTERCEPTORS FROM CleanAspire project in Infrastructure to CleanArch separation
//and not edit DbContext overloading SaveChanges etc + using DI