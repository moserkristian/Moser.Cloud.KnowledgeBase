using Microsoft.Extensions.VectorData;

using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

internal sealed class InMemoryDocumentIndex : IDocumentIndex
{
    private const string CollectionName = "policy-chunks";
    private readonly VectorStore _store;
    private readonly object _gate = new();
    private VectorStoreCollection<string, PolicyChunkRecord>? _collection;
    private int _dimensions;
    private readonly List<IndexedChunk> _all = [];
    private TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _count;

    public InMemoryDocumentIndex(VectorStore vectorStore)
    {
        _store = vectorStore;
    }

    public async Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        var collection = await CollectionAsync(chunks[0].Embedding.Length, cancellationToken).ConfigureAwait(false);
        var records = new List<PolicyChunkRecord>(chunks.Count);
        foreach (var chunk in chunks)
        {
            records.Add(new PolicyChunkRecord
            {
                Id = chunk.Id,
                Source = chunk.Source,
                Content = chunk.Content,
                Embedding = chunk.Embedding
            });
        }

        await collection.UpsertAsync(records, cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            _all.AddRange(chunks);
            _count += chunks.Count;
        }
    }

    public async Task<IReadOnlyList<IndexedChunk>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int top,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IndexedChunk>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (_collection is not null)
        {
            await foreach (var hit in _collection.SearchAsync(queryEmbedding, top, options: null, cancellationToken).ConfigureAwait(false))
            {
                seen.Add(hit.Record.Id);
                results.Add(new IndexedChunk(
                    hit.Record.Id,
                    hit.Record.Source,
                    hit.Record.Content,
                    hit.Record.Embedding,
                    hit.Score));
            }
        }

        List<IndexedChunk> snapshot;
        lock (_gate)
        {
            snapshot = [.. _all];
        }

        foreach (var chunk in snapshot)
        {
            if (!seen.Add(chunk.Id))
            {
                continue;
            }

            var score = Cosine(queryEmbedding.Span, chunk.Embedding.Span);
            if (score < 0.02)
            {
                continue;
            }

            results.Add(new IndexedChunk(chunk.Id, chunk.Source, chunk.Content, chunk.Embedding, score));
        }

        return results
            .OrderByDescending(r => r.Score ?? 0)
            .Take(Math.Max(top, 12))
            .ToList();
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_count);

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_collection is not null)
        {
            await _store.EnsureCollectionDeletedAsync(CollectionName, cancellationToken).ConfigureAwait(false);
        }

        lock (_gate)
        {
            _all.Clear();
            _count = 0;
            _collection = null;
            _dimensions = 0;
        }
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

    private async Task<VectorStoreCollection<string, PolicyChunkRecord>> CollectionAsync(int dimensions, CancellationToken cancellationToken)
    {
        if (_collection is not null && _dimensions == dimensions)
        {
            return _collection;
        }

        if (_collection is not null)
        {
            await _store.EnsureCollectionDeletedAsync(CollectionName, cancellationToken).ConfigureAwait(false);
            _collection = null;
        }

        var definition = new VectorStoreCollectionDefinition
        {
            Properties =
            [
                new VectorStoreKeyProperty(nameof(PolicyChunkRecord.Id), typeof(string)),
                new VectorStoreDataProperty(nameof(PolicyChunkRecord.Source), typeof(string)),
                new VectorStoreDataProperty(nameof(PolicyChunkRecord.Content), typeof(string)),
                new VectorStoreVectorProperty(nameof(PolicyChunkRecord.Embedding), typeof(ReadOnlyMemory<float>), dimensions)
                {
                    DistanceFunction = DistanceFunction.CosineSimilarity
                }
            ]
        };

        var collection = _store.GetCollection<string, PolicyChunkRecord>(CollectionName, definition);
        await collection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
        _collection = collection;
        _dimensions = dimensions;
        return collection;
    }

    private static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var len = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na == 0 || nb == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
