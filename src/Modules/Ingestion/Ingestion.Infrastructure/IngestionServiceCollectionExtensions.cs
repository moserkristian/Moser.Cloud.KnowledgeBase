using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moser.RagAi.Ingestion.Application;

using Npgsql;

using Pgvector.Npgsql;

using System;

namespace Moser.RagAi.Ingestion.Infrastructure;

public static class IngestionServiceCollectionExtensions
{
    public static IServiceCollection AddIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connection = configuration.GetConnectionString("rag")
            ?? configuration.GetConnectionString("postgres");

        if (!string.IsNullOrWhiteSpace(connection))
        {
            var builder = new NpgsqlDataSourceBuilder(connection);
            builder.UseVector();
            services.AddSingleton(builder.Build());
            services.AddSingleton<IDocumentIndex, PgVectorDocumentIndex>();
        }
        else
        {
            services.AddSingleton<IDocumentIndex, MemoryDocumentIndex>();
        }

        services.AddSingleton<IPolicyDocumentReader, CompositeDocumentReader>();
        services.AddSingleton<IPolicyChunker, SentenceOverlapChunker>();
        services.AddSingleton<ISourceLibrary, SourceLibrary>();
        services.AddSingleton<IOfficeSeedPack, OfficeSeedPack>();
        services.AddSingleton<IngestSeedHandler>();
        return services;
    }
}
