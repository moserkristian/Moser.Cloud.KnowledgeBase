using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.Configuration;

using Moser.Enterprise.Blueprint.Catalog.Infrastructure;

namespace Moser.Enterprise.Blueprint.AppHost;

public static class Program
{
    public const string PostgresContainerName = "platform-postgres-container";
    public const string PostgresDbName = "postgres-db";
    public const string CatalogApiName = "catalog-api";
    public const string WebFrontendName = "webfrontend";
    public const string OllamaName = "ollama";
    public const string OllamaUrl = "http://127.0.0.1:11434";

    static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var cache = builder.AddRedis("cache");
        var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api");

        var web = builder.AddProject<Projects.Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(catalogApi)
            .WaitFor(catalogApi)
            .WithEnvironment("OLLAMA_CHAT_MODEL", "llama3.2")
            .WithEnvironment("OLLAMA_EMBED_MODEL", "nomic-embed-text");

        // Default (local): native Ollama at 127.0.0.1:11434, same as before.
        // CI fixture CiDistributedAppTestFixture passes Ollama:Enabled=false so Web starts on the stub.
        if (builder.Configuration.GetValue("Ollama:Enabled", true))
        {
            var ollama = builder.AddExternalService(OllamaName, OllamaUrl)
                .WithHttpHealthCheck("/api/tags");
            web.WithReference(ollama)
                .WithEnvironment("OLLAMA_ENDPOINT", OllamaUrl);
        }

        builder.Build().Run();
    }

    public static IResourceBuilder<PostgresServerResource> AddPostgresContainer(IDistributedApplicationBuilder builder, bool includePostgresAdministrationPlatform = false)
    {
        var postgresServerBuilder = builder.AddPostgres(PostgresContainerName);

        if (includePostgresAdministrationPlatform)
        {
            postgresServerBuilder.WithPgAdmin();
        }

        return postgresServerBuilder;
    }

    public static IResourceBuilder<PostgresDatabaseResource> AddPostgresDb(IResourceBuilder<PostgresServerResource> postgresContainer)
    {
        return postgresContainer.AddDatabase(PostgresDbName, FakeDbContext.DbName);
    }
}
