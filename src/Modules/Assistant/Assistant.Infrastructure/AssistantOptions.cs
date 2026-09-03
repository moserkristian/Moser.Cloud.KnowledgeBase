namespace Moser.RagAi.Assistant.Infrastructure;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>Root folder that contains per-scenario seed directories.</summary>
    public string SeedRoot { get; set; } = "data/seed";

    /// <summary>Default corpus: Legal, RealEstate, Healthcare, Finance, Insurance, Consulting, Corporate.</summary>
    public string Scenario { get; set; } = "Legal";

    /// <summary>Legacy single-folder path (still honored if Scenario is unset / unknown).</summary>
    public string SeedPath { get; set; } = "data/seed/legal";

    public string? Endpoint { get; set; }
    public string? OllamaEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string ChatModel { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    /// <summary>pgvector when a rag connection string is present; otherwise cosine memory.</summary>
    public string VectorStore { get; set; } = "pgvector";
    public int MaxTokensPerChunk { get; set; } = 640;
    public int OverlapTokens { get; set; } = 96;
}
