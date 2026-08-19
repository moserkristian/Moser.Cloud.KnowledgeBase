using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

using CommunityToolkit.VectorData.InMemory;

using Moser.Enterprise.Blueprint.Ingestion.Application;

using System;

namespace Moser.Enterprise.Blueprint.Ingestion.Infrastructure;

public static class IngestionServiceCollectionExtensions
{
    public const string VectorStoreConfigKey = "Assistant:VectorStore";

    public static IServiceCollection AddIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(_ => CreateVectorStore(configuration));
        services.AddSingleton<IDocumentIndex, InMemoryDocumentIndex>();
        services.AddSingleton<IPolicyDocumentReader, MarkdownPolicyDocumentReader>();
        services.AddSingleton<IPolicyChunker, MediTokenChunker>();
        services.AddSingleton<ISeedFaqSynthesizer, SeedSynthesizer>();
        services.AddSingleton<IngestSeedHandler>();
        return services;
    }

    private static VectorStore CreateVectorStore(IConfiguration configuration)
    {
        var kind = configuration[VectorStoreConfigKey] ?? "InMemory";
        if (kind.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return new InMemoryVectorStore(new InMemoryVectorStoreOptions());
        }

        throw new NotSupportedException(
            $"Assistant:VectorStore '{kind}' is not wired. Keep InMemory, or add an IDocumentIndex for pgvector.");
    }
}
