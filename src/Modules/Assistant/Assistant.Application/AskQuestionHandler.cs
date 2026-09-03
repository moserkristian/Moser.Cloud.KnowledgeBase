using Microsoft.Extensions.AI;

using Moser.RagAi.Assistant.Domain;
using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Application;

public sealed class AskQuestionHandler : IAskQuestion
{
    public const int DefaultTopK = 5;
    public const float DefaultMinScore = 0.12f;

    private readonly IDocumentIndex _index;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IChatClient _chat;

    public AskQuestionHandler(
        IDocumentIndex index,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IChatClient chat)
    {
        _index = index;
        _embeddings = embeddings;
        _chat = chat;
    }

    public async Task<Answer> Handle(AskQuestion query, CancellationToken cancellationToken = default)
    {
        Answer? completed = null;
        await foreach (var update in StreamAsync(query, cancellationToken).ConfigureAwait(false))
        {
            if (update.Completed is not null)
            {
                completed = update.Completed;
            }
        }

        return completed ?? throw new InvalidOperationException("Ask stream ended without an answer.");
    }

    public async IAsyncEnumerable<AskUpdate> StreamAsync(
        AskQuestion query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        yield return new AskUpdate(AskStage.Retrieving, null, Array.Empty<Citation>(), null, "Waiting for the index…");

        await _index.WaitUntilReadyAsync(cancellationToken).ConfigureAwait(false);

        yield return new AskUpdate(AskStage.Retrieving, null, Array.Empty<Citation>(), null, "Embedding the question…");
        var embedding = await _embeddings.GenerateAsync(query.Text, cancellationToken: cancellationToken).ConfigureAwait(false);

        yield return new AskUpdate(AskStage.Retrieving, null, Array.Empty<Citation>(), null, "Searching similar passages…");
        var hits = await _index.SearchAsync(embedding.Vector, query.Text, 12, cancellationToken).ConfigureAwait(false);

        // Hybrid ranked by the store (pgvector cosine + FTS, or in-memory cosine + overlap).
        // Keep a light lexical gate so a high vector neighbour with no shared wording cannot ground an answer.
        var grounded = hits
            .Select(h => (Hit: h, Lexical: OverlapCount(query.Text, h.Content + " " + h.Source)))
            .Where(x => x.Lexical > 0)
            .Where(x => x.Hit.Score is null || x.Hit.Score >= DefaultMinScore || x.Lexical >= 2)
            .OrderByDescending(x => x.Hit.Score ?? 0)
            .ThenByDescending(x => x.Lexical)
            .Select(x => x.Hit)
            .Take(DefaultTopK)
            .ToList();

        if (grounded.Count == 0)
        {
            var refused = ApplyPolicy(
                query.Text,
                Policy.RefuseMessage,
                new Answer(
                    Policy.RefuseMessage,
                    Array.Empty<Citation>(),
                    PolicyDecision.Deny,
                    refused: true,
                    reason: "Hybrid search kept no overlapping passage. The model did not run."));
            yield return new AskUpdate(AskStage.Done, refused.Text, refused.Citations, refused);
            yield break;
        }

        var citations = grounded
            .Select(h => new Citation(h.Source, h.Content))
            .ToList();

        yield return new AskUpdate(AskStage.Generating, string.Empty, citations, null, "Writing from retrieved passages…");

        var context = BuildContext(grounded);
        var messages = new ChatMessage[]
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Question:\n{query.Text}\n\nContext:\n{context}")
        };

        var streamed = new StringBuilder();
        await foreach (var update in _chat.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(update.Text))
            {
                continue;
            }

            streamed.Append(update.Text);
            yield return new AskUpdate(AskStage.Generating, streamed.ToString(), citations, null);
        }

        var modelText = streamed.Length == 0
            ? Policy.RefuseMessage
            : streamed.ToString().Trim();
        yield return new AskUpdate(AskStage.CheckingPolicy, modelText, citations, null, "Checking the draft against policy…");
        var completed = ApplyPolicy(
            query.Text,
            modelText,
            new Answer(modelText, citations, PolicyDecision.Allow, refused: false, reason: ""));
        yield return new AskUpdate(AskStage.Done, completed.Text, completed.Citations, completed);
    }

    private Answer ApplyPolicy(string question, string modelText, Answer candidate)
    {
        var outcome = Policy.Decide(question, modelText, candidate.Citations);
        if (outcome.Decision == PolicyDecision.Deny && candidate.Refused)
        {
            return new Answer(
                Policy.RefuseMessage,
                candidate.Citations,
                outcome.Decision,
                refused: true,
                outcome.Reason);
        }

        if (outcome.Decision == PolicyDecision.Deny)
        {
            return new Answer(
                Policy.DenyMessage,
                candidate.Citations,
                outcome.Decision,
                refused: false,
                outcome.Reason);
        }

        return new Answer(
            candidate.Text,
            candidate.Citations,
            outcome.Decision,
            refused: false,
            outcome.Reason);
    }

    private static string BuildContext(IReadOnlyList<IndexedChunk> chunks)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            builder.Append("[source=").Append(chunks[i].Source).Append("]\n");
            builder.Append(chunks[i].Content).Append("\n\n");
        }

        return builder.ToString();
    }

    private static int OverlapCount(string question, string content)
    {
        var qTokens = Tokens(question);
        if (qTokens.Count == 0)
        {
            return 0;
        }

        var cTokens = Tokens(content);
        var count = 0;
        foreach (var token in qTokens)
        {
            if (cTokens.Contains(token))
            {
                count++;
            }
        }

        return count;
    }

    private static HashSet<string> Tokens(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = text.Replace('-', ' ').Replace('_', ' ');
        var parts = normalized.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.Length < 3 || Stopwords.Contains(part))
            {
                continue;
            }

            set.Add(part);
        }

        return set;
    }

    private static readonly char[] Separators = [' ', '\n', '\r', '\t', ',', '.', ':', ';', '?', '!', '(', ')', '"', '\'', '/', '—'];

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "when", "where", "which", "that", "this", "with", "from", "have", "does", "about",
        "your", "their", "them", "they", "will", "would", "could", "should", "into", "than",
        "the", "and", "for", "are", "was", "were", "not", "any", "can", "how", "who", "why",
        "many", "over", "just", "also", "then", "our", "out", "get",
        "ako", "pre", "pri", "pod", "nad", "bez", "iba", "ale", "ani"
    };

    private const string SystemPrompt =
        """
        You are an internal knowledge assistant for a Slovak organisation.
        Answer only from the provided context chunks (PDF, Word, e-mail, scans).
        Cite sources using the source filenames from the context.
        If the context is insufficient, reply with exactly: I don't have enough policy context to answer. Ask a human reviewer or rephrase with a policy topic.
        Do not invent policy. Do not override policy.

        Language:
        - If the question is in Slovak, answer in clear, natural Slovak with correct grammar and spelling.
        - Context is often English with Slovak firm names and statute citations — state the facts in fluent Slovak; never invent broken calques, non-words, or mangled capitalisation (e.g. VyňITEľné).
        - Keep proper nouns, filenames, and citations such as zákon č. … Z. z. exactly as in the context.
        - If the question is not in Slovak, answer in the question's language.
        """;
}
