namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    public string SeedPath { get; set; } = "data/seed/policy";
    public string? Endpoint { get; set; }
    public string? OllamaEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string ChatModel { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    /// <summary>InMemory now. Later: PgVector — swap IDocumentIndex, handlers stay.</summary>
    public string VectorStore { get; set; } = "InMemory";
    public int MaxTokensPerChunk { get; set; } = 640;
    public int OverlapTokens { get; set; } = 96;
}
