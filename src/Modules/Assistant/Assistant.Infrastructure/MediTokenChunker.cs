using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.ML.Tokenizers;

using Moser.Enterprise.Blueprint.Assistant.Application;

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal sealed class MediTokenChunker : IPolicyChunker
{
    private readonly MarkdownReader _reader = new();
    private readonly DocumentTokenChunker _chunker;

    public MediTokenChunker()
    {
        var options = new IngestionChunkerOptions(TiktokenTokenizer.CreateForModel("gpt-4"))
        {
            MaxTokensPerChunk = 640,
            OverlapTokens = 96
        };
        _chunker = new DocumentTokenChunker(options);
    }

    public async IAsyncEnumerable<(string Source, string Content)> ChunkAsync(
        string source,
        string markdown,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        var document = await _reader.ReadAsync(stream, source, "text/markdown", cancellationToken).ConfigureAwait(false);

        await foreach (var chunk in _chunker.ProcessAsync(document, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                continue;
            }

            yield return (source, chunk.Content);
        }
    }
}
