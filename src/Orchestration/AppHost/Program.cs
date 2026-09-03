using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Moser.RagAi.AppHost;

public static class Program
{
    public const string WebFrontendName = "webfrontend";
    public const string OllamaName = "ollama";
    public const string PostgresName = "postgres";
    public const string RagDatabaseName = "rag";
    public const string OllamaUrl = "http://127.0.0.1:11434";

    static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var postgres = builder.AddPostgres(PostgresName)
            .WithImage("pgvector/pgvector", "pg17")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithDataVolume()
            .AddDatabase(RagDatabaseName);

        // Native Windows Ollama (binds 127.0.0.1:11434). Do not add a Docker/Aspire Ollama
        // container — it would clash on 11434 with the installed app/service.
        // Do not WaitFor it: Web probes the endpoint at startup and falls back to stub.
        var ollama = builder.AddExternalService(OllamaName, OllamaUrl);

        builder.AddProject<Projects.Web>(WebFrontendName)
            .WithExternalHttpEndpoints()
            .WithReference(postgres)
            .WaitFor(postgres)
            .WithReference(ollama)
            .WithEnvironment("OLLAMA_ENDPOINT", OllamaUrl)
            .WithEnvironment("OLLAMA_CHAT_MODEL", "llama3.2")
            .WithEnvironment("OLLAMA_EMBED_MODEL", "nomic-embed-text");

        builder.Build().Run();
    }
}
