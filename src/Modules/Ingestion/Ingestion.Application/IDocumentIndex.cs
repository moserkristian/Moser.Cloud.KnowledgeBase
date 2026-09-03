using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Application;

/// <summary>
/// Vector store seam. PostgreSQL + pgvector when Aspire injects a connection
/// string; a cosine memory index otherwise (evals / Web without Docker).
/// </summary>
public interface IDocumentIndex
{
    string Provider { get; }

    Task InitializeAsync(int dimensions, CancellationToken cancellationToken = default);

    Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IndexedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string queryText,
        int top,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IndexedChunk>> ListAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    void BeginReset();

    void MarkReady();
}
