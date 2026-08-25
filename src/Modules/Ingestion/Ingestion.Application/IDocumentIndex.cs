using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Application;

/// <summary>
/// Vector store seam written by ingest and read by Assistant.
/// InMemory today; a pgvector type can replace this registration without changing Ask/Ingest.
/// </summary>
public interface IDocumentIndex
{
    Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexedChunk>> SearchAsync(ReadOnlyMemory<float> queryEmbedding, int top, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexedChunk>> ListAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
    void BeginReset();
    void MarkReady();
}
