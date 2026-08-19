using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Moser.Enterprise.Blueprint.AppHost;

public static class Program
{
    public const string PostgresContainerName = "platform-postgres-container";
    public const string PostgresDbName = "postgres-db";
    public const string PeopleApiName = "people-api";
    public const string WebFrontendName = "webfrontend";
    public const string OllamaName = "ollama";
    public const string OllamaUrl = "http://127.0.0.1:11434";

    static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var cache = builder.AddRedis("cache");

        // Stateless employee directory. Replicas share the same in-memory seed — safe to scale out
        // when many employees look up “who owns PTO?” independently of the Blazor Web process.
        var peopleApi = builder.AddProject<Projects.People_API>(PeopleApiName)
            .WithReplicas(2);

        // Native Windows Ollama (binds 127.0.0.1:11434). Do not add a Docker/Aspire Ollama
        // container — it would clash on 11434 with the installed app/service.
        // Do not WaitFor it: Web probes the endpoint at startup and falls back to stub.
        var ollama = builder.AddExternalService(OllamaName, OllamaUrl);

        builder.AddProject<Projects.Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(peopleApi)
            .WaitFor(peopleApi)
            .WithReference(ollama)
            .WithEnvironment("OLLAMA_ENDPOINT", OllamaUrl)
            .WithEnvironment("OLLAMA_CHAT_MODEL", "llama3.2")
            .WithEnvironment("OLLAMA_EMBED_MODEL", "nomic-embed-text");

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
}
