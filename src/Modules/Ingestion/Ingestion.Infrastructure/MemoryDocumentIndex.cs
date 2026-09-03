using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

/// <summary>
/// Cosine + lexical hybrid for tests and runs without Postgres.
/// </summary>
internal sealed class MemoryDocumentIndex : IDocumentIndex
{
    private readonly object _gate = new();
    private readonly List<IndexedChunk> _all = [];
    private TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string Provider => "memory";

    public Task InitializeAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dimensions;
        return Task.CompletedTask;
    }

    public Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (chunks.Count == 0)
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            _all.AddRange(chunks);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IndexedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        string queryText,
        int top,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        top = Math.Max(top, 1);
        List<IndexedChunk> snapshot;
        lock (_gate)
        {
            snapshot = [.. _all];
        }

        var ranked = snapshot
            .Select(chunk =>
            {
                var vector = Cosine(queryEmbedding.Span, chunk.Embedding.Span);
                var lexical = LexicalScore(queryText, chunk.Content + " " + chunk.Source);
                var combined = (vector * 0.7) + (lexical * 0.3);
                return new IndexedChunk(chunk.Id, chunk.Source, chunk.Content, chunk.Embedding, combined);
            })
            .OrderByDescending(c => c.Score ?? 0)
            .Take(top)
            .ToList();

        return Task.FromResult<IReadOnlyList<IndexedChunk>>(ranked);
    }

    public Task<IReadOnlyList<IndexedChunk>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<IndexedChunk>>([.. _all]);
        }
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(_all.Count);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _all.Clear();
        }

        return Task.CompletedTask;
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

    internal static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom <= 0 ? 0 : dot / denom;
    }

    internal static double LexicalScore(string query, string content)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var q = Tokens(query);
        if (q.Count == 0)
        {
            return 0;
        }

        var c = Tokens(content);
        var hits = 0;
        foreach (var token in q)
        {
            if (c.Contains(token))
            {
                hits++;
            }
        }

        return (double)hits / q.Count;
    }

    private static HashSet<string> Tokens(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = text.Replace('-', ' ').Replace('_', ' ');
        foreach (var part in normalized.Split(
                     [' ', '\n', '\r', '\t', ',', '.', ':', ';', '?', '!', '(', ')', '"', '\'', '/', '—'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length >= 3)
            {
                set.Add(part);
            }
        }

        return set;
    }
}
