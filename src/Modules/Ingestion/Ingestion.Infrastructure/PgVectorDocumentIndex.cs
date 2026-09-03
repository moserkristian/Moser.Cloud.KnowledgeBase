using Moser.RagAi.Ingestion.Application;

using Npgsql;

using Pgvector;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

/// <summary>
/// PostgreSQL + pgvector store. Hybrid search is vector cosine + simple FTS
/// (Milan Jovanović RAG pipeline; HNSW instead of IVFFlat for this corpus size).
/// </summary>
internal sealed class PgVectorDocumentIndex : IDocumentIndex
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly object _gate = new();
    private TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _dimensions;
    private bool _initialized;

    public PgVectorDocumentIndex(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string Provider => "pgvector";

    public async Task InitializeAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        if (dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions));
        }

        lock (_gate)
        {
            if (_initialized && _dimensions == dimensions)
            {
                return;
            }
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (var ext = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector;", conn))
        {
            await ext.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await conn.ReloadTypesAsync();

        var needReset = false;
        lock (_gate)
        {
            needReset = _initialized && _dimensions != dimensions;
        }

        if (needReset)
        {
            await using var drop = new NpgsqlCommand("DROP TABLE IF EXISTS document_chunks;", conn);
            await drop.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var vectorType = $"vector({dimensions})";
        await using (var ddl = new NpgsqlCommand($"""
            CREATE TABLE IF NOT EXISTS document_chunks (
                id TEXT PRIMARY KEY,
                source TEXT NOT NULL,
                content TEXT NOT NULL,
                embedding {vectorType} NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """, conn))
        {
            await ddl.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var idx = new NpgsqlCommand("""
            CREATE INDEX IF NOT EXISTS document_chunks_embedding_hnsw
                ON document_chunks USING hnsw (embedding vector_cosine_ops);
            CREATE INDEX IF NOT EXISTS document_chunks_fts
                ON document_chunks USING gin (to_tsvector('simple', content));
            """, conn))
        {
            await idx.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        lock (_gate)
        {
            _dimensions = dimensions;
            _initialized = true;
        }
    }

    public async Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await InitializeAsync(chunks[0].Embedding.Length, cancellationToken).ConfigureAwait(false);

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var chunk in chunks)
        {
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO document_chunks (id, source, content, embedding)
                VALUES ($1, $2, $3, $4)
                ON CONFLICT (id) DO UPDATE SET
                    source = EXCLUDED.source,
                    content = EXCLUDED.content,
                    embedding = EXCLUDED.embedding
                """, conn, tx);
            cmd.Parameters.AddWithValue(chunk.Id);
            cmd.Parameters.AddWithValue(chunk.Source);
            cmd.Parameters.AddWithValue(chunk.Content);
            cmd.Parameters.AddWithValue(new Vector(chunk.Embedding.ToArray()));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IndexedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string queryText,
        int top,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            return [];
        }

        top = Math.Max(top, 1);
        var tsQuery = ToTsQuery(queryText);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand("""
            WITH vector_results AS (
                SELECT id, source, content, embedding,
                       1 - (embedding <=> $1) AS vector_score
                FROM document_chunks
                ORDER BY embedding <=> $1
                LIMIT $3
            ),
            text_results AS (
                SELECT id, source, content, embedding,
                       ts_rank(to_tsvector('simple', content), plainto_tsquery('simple', $2)) AS text_score
                FROM document_chunks
                WHERE $2 <> ''
                  AND to_tsvector('simple', content) @@ plainto_tsquery('simple', $2)
                LIMIT $3
            )
            SELECT COALESCE(v.id, t.id) AS id,
                   COALESCE(v.source, t.source) AS source,
                   COALESCE(v.content, t.content) AS content,
                   COALESCE(v.embedding, t.embedding) AS embedding,
                   COALESCE(v.vector_score, 0) * 0.7 + COALESCE(t.text_score, 0) * 0.3 AS score
            FROM vector_results v
            FULL OUTER JOIN text_results t ON v.id = t.id
            ORDER BY score DESC
            LIMIT $3
            """, conn);
        cmd.Parameters.AddWithValue(new Vector(queryEmbedding.ToArray()));
        cmd.Parameters.AddWithValue(tsQuery);
        cmd.Parameters.AddWithValue(top);

        var results = new List<IndexedChunk>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var vector = reader.GetFieldValue<Vector>(3);
            results.Add(new IndexedChunk(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                vector.ToArray(),
                reader.GetDouble(4)));
        }

        return results;
    }

    public async Task<IReadOnlyList<IndexedChunk>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            return [];
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, source, content, embedding FROM document_chunks ORDER BY source, id;",
            conn);
        var results = new List<IndexedChunk>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var vector = reader.GetFieldValue<Vector>(3);
            results.Add(new IndexedChunk(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                vector.ToArray()));
        }

        return results;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            return 0;
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM document_chunks;", conn);
        var value = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            return;
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand("TRUNCATE document_chunks;", conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
        => _ready.Task.WaitAsync(cancellationToken);

    public void BeginReset()
    {
        lock (_gate)
        {
            _ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public void MarkReady() => _ready.TrySetResult();

    private static string ToTsQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(query, @"[^\p{L}\p{Nd}\s]+", " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }
}
