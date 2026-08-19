using System;

namespace Moser.Enterprise.Blueprint.Ingestion.Application;

public sealed class IndexedChunk
{
    public IndexedChunk(string id, string source, string content, ReadOnlyMemory<float> embedding, double? score = null)
    {
        Id = id;
        Source = source;
        Content = content;
        Embedding = embedding;
        Score = score;
    }

    public string Id { get; }
    public string Source { get; }
    public string Content { get; }
    public ReadOnlyMemory<float> Embedding { get; }
    public double? Score { get; }
}
