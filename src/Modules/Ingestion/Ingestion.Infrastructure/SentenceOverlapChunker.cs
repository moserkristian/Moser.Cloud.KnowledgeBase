using Microsoft.ML.Tokenizers;

using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

/// <summary>
/// Sentence-window chunker with token overlap (Milan Jovanović / RAG-in-.NET pattern).
/// Character length is a poor proxy for Slovak legal prose; tiktoken counts tokens.
/// </summary>
internal sealed class SentenceOverlapChunker : IPolicyChunker
{
    private readonly Tokenizer _tokenizer;
    private readonly int _maxTokens;
    private readonly int _overlapTokens;

    public SentenceOverlapChunker(int maxTokens = 640, int overlapTokens = 96)
    {
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
        _maxTokens = maxTokens;
        _overlapTokens = overlapTokens;
    }

    public async IAsyncEnumerable<(string Source, string Content)> ChunkAsync(
        string source,
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var sentences = SplitSentences(text);
        var current = new StringBuilder();
        var overlap = new Queue<string>();
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = current.Length == 0 ? sentence : current + " " + sentence;
            if (current.Length > 0 && CountTokens(candidate) > _maxTokens)
            {
                var piece = current.ToString().Trim();
                if (piece.Length > 0)
                {
                    yield return (source, piece);
                    chunkIndex++;
                }

                current.Clear();
                foreach (var kept in overlap)
                {
                    if (current.Length > 0)
                    {
                        current.Append(' ');
                    }

                    current.Append(kept);
                }
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(sentence);
            overlap.Enqueue(sentence);
            while (overlap.Count > 0 && CountTokens(string.Join(' ', overlap)) > _overlapTokens)
            {
                overlap.Dequeue();
            }
        }

        var tail = current.ToString().Trim();
        if (tail.Length > 0)
        {
            yield return (source, tail);
        }

        _ = chunkIndex;
    }

    private int CountTokens(string value)
        => _tokenizer.CountTokens(value);

    private static List<string> SplitSentences(string text)
    {
        var parts = text.Split(
            [". ", "! ", "? ", ".\n", "!\n", "?\n", "\n\n"],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                list.Add(part);
            }
        }

        return list.Count == 0 ? [text.Trim()] : list;
    }
}
