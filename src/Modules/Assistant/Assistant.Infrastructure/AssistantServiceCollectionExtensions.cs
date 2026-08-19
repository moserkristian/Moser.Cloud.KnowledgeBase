using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moser.Enterprise.Blueprint.Assistant.Application;
using Moser.Enterprise.Blueprint.Ingestion.Infrastructure;

using System;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

public static class AssistantServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddAssistant(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddAssistantCore(builder.Configuration);
        return builder;
    }

    public static IServiceCollection AddAssistantCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AssistantOptions>(configuration.GetSection(AssistantOptions.SectionName));
        services.AddIngestion(configuration);

        services.AddSingleton(_ => AiClientFactory.Create(configuration));
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Chat);
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Embeddings);
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Info);

        services.AddSingleton<IAskQuestion, AskQuestionHandler>();
        services.AddSingleton<AssistantWorkspace>();
        services.AddSingleton<IAssistantWorkspace>(sp => sp.GetRequiredService<AssistantWorkspace>());
        services.AddHostedService<SeedIngestionHostedService>();
        return services;
    }
}
