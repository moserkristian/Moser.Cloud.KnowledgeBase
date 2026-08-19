using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;

using CommunityToolkit.VectorData.InMemory;

using Moser.Enterprise.Blueprint.Assistant.Application;

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

        services.AddSingleton(_ => AiClientFactory.Create(configuration));
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Chat);
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Embeddings);
        services.AddSingleton(sp => sp.GetRequiredService<AiStack>().Info);

        services.AddSingleton(CreateVectorStore);
        services.AddSingleton<IDocumentIndex, InMemoryDocumentIndex>();
        services.AddSingleton<IPolicyDocumentReader, MarkdownPolicyDocumentReader>();
        services.AddSingleton<IPolicyChunker, MediTokenChunker>();
        services.AddSingleton<ISeedFaqSynthesizer, SeedSynthesizer>();
        services.AddSingleton<IngestSeedHandler>();
        services.AddSingleton<IAskQuestion, AskQuestionHandler>();
        services.AddSingleton<AssistantWorkspace>();
        services.AddSingleton<IAssistantWorkspace>(sp => sp.GetRequiredService<AssistantWorkspace>());
        services.AddHostedService<SeedIngestionHostedService>();
        return services;
    }

    private static VectorStore CreateVectorStore(IServiceProvider services)
    {
        var kind = services.GetRequiredService<IConfiguration>()[$"{AssistantOptions.SectionName}:VectorStore"] ?? "InMemory";
        if (kind.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return new InMemoryVectorStore(new InMemoryVectorStoreOptions());
        }

        throw new NotSupportedException(
            $"Assistant:VectorStore '{kind}' is not wired. Keep InMemory, or add an IDocumentIndex for pgvector.");
    }
}
