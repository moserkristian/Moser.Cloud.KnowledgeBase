using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Application;

public sealed record IngestSeed(string? SeedDirectory = null);

public interface IPolicyDocumentReader
{
    IAsyncEnumerable<(string Source, string Markdown)> ReadAsync(string directory, CancellationToken cancellationToken = default);
}

public interface IPolicyChunker
{
    IAsyncEnumerable<(string Source, string Content)> ChunkAsync(string source, string markdown, CancellationToken cancellationToken = default);
}

public interface ISeedFaqSynthesizer
{
    IReadOnlyList<(string Source, string Markdown)> Synthesize();
}

public sealed class IngestSeedHandler
{
    private readonly IDocumentIndex _index;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IPolicyDocumentReader _reader;
    private readonly IPolicyChunker _chunker;
    private readonly ISeedFaqSynthesizer _synthesizer;

    public IngestSeedHandler(
        IDocumentIndex index,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IPolicyDocumentReader reader,
        IPolicyChunker chunker,
        ISeedFaqSynthesizer synthesizer)
    {
        _index = index;
        _embeddings = embeddings;
        _reader = reader;
        _chunker = chunker;
        _synthesizer = synthesizer;
    }

    public async Task Handle(IngestSeed command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var records = new List<(string Source, string Content)>();

            if (!string.IsNullOrWhiteSpace(command.SeedDirectory) && Directory.Exists(command.SeedDirectory))
            {
                await foreach (var document in _reader.ReadAsync(command.SeedDirectory, cancellationToken).ConfigureAwait(false))
                {
                    await foreach (var chunk in _chunker.ChunkAsync(document.Source, document.Markdown, cancellationToken).ConfigureAwait(false))
                    {
                        records.Add(chunk);
                    }
                }
            }

            foreach (var faq in _synthesizer.Synthesize().Take(50))
            {
                await foreach (var chunk in _chunker.ChunkAsync(faq.Source, faq.Markdown, cancellationToken).ConfigureAwait(false))
                {
                    records.Add(chunk);
                }
            }

            if (records.Count == 0)
            {
                return;
            }

            var embeddings = await _embeddings.GenerateAsync(records.Select(r => r.Content), cancellationToken: cancellationToken).ConfigureAwait(false);
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
            _index.MarkReady();
        }
    }
}
