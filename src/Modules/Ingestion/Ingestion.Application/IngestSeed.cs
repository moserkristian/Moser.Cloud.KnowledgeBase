using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Application;

public sealed record IngestSeed(string? SeedDirectory = null);

public interface IPolicyDocumentReader
{
    IAsyncEnumerable<SourceDocument> ReadAsync(string directory, CancellationToken cancellationToken = default);
}

public interface IPolicyChunker
{
    IAsyncEnumerable<(string Source, string Content)> ChunkAsync(
        string source,
        string text,
        CancellationToken cancellationToken = default);
}

public interface IOfficeSeedPack
{
    Task MaterializeAsync(string directory, CancellationToken cancellationToken = default);
}

public sealed class IngestSeedHandler
{
    private readonly IDocumentIndex _index;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IPolicyDocumentReader _reader;
    private readonly IPolicyChunker _chunker;
    private readonly ISourceLibrary _sources;
    private readonly IOfficeSeedPack _seedPack;

    public IngestSeedHandler(
        IDocumentIndex index,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IPolicyDocumentReader reader,
        IPolicyChunker chunker,
        ISourceLibrary sources,
        IOfficeSeedPack seedPack)
    {
        _index = index;
        _embeddings = embeddings;
        _reader = reader;
        _chunker = chunker;
        _sources = sources;
        _seedPack = seedPack;
    }

    public async Task Handle(IngestSeed command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ingested = new List<SourceDocument>();
        try
        {
            var records = new List<(string Source, string Content)>();

            if (!string.IsNullOrWhiteSpace(command.SeedDirectory) && Directory.Exists(command.SeedDirectory))
            {
                await _seedPack.MaterializeAsync(command.SeedDirectory, cancellationToken).ConfigureAwait(false);
                await foreach (var document in _reader.ReadAsync(command.SeedDirectory, cancellationToken).ConfigureAwait(false))
                {
                    ingested.Add(document);
                    await foreach (var chunk in _chunker.ChunkAsync(document.FileName, document.ExtractedText, cancellationToken).ConfigureAwait(false))
                    {
                        records.Add(chunk);
                    }
                }
            }

            if (records.Count == 0)
            {
                return;
            }

            var embeddings = await _embeddings.GenerateAsync(records.Select(r => r.Content), cancellationToken: cancellationToken).ConfigureAwait(false);
            await _index.InitializeAsync(embeddings[0].Vector.Length, cancellationToken).ConfigureAwait(false);
            var chunks = new List<IndexedChunk>(records.Count);
            for (var i = 0; i < records.Count; i++)
            {
                var (source, content) = records[i];
                chunks.Add(new IndexedChunk($"{source}#{i}", source, content, embeddings[i].Vector));
            }

            await _index.UpsertAsync(chunks, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sources.Replace(ingested);
            _index.MarkReady();
        }
    }
}
