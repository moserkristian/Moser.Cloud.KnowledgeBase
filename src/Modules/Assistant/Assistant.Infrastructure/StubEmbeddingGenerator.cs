using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal sealed class StubEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int Dimensions = 64;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(new Embedding<float>(Embed(value ?? string.Empty)));
        }

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return new EmbeddingGeneratorMetadata("stub", null, "hash-bag-of-words", Dimensions);
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    internal static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        var tokens = text.ToLowerInvariant().Split(
            [' ', '\n', '\r', '\t', ',', '.', ':', ';', '?', '!', '(', ')', '"', '\'', '/', '-', '_'],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            vector[Hash(token) % Dimensions] += 1f;
            if (token.Length >= 3)
            {
                for (var i = 0; i <= token.Length - 3; i++)
                {
                    vector[Hash(token.AsSpan(i, 3)) % Dimensions] += 0.35f;
                }
            }
        }

        var norm = 0d;
        for (var i = 0; i < vector.Length; i++)
        {
            norm += vector[i] * vector[i];
        }

        if (norm > 0)
        {
            var scale = (float)(1d / Math.Sqrt(norm));
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] *= scale;
            }
        }

        return vector;
    }

    private static int Hash(string value)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in value)
            {
                hash = (hash ^ c) * 16777619;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static int Hash(ReadOnlySpan<char> value)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in value)
            {
                hash = (hash ^ c) * 16777619;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
