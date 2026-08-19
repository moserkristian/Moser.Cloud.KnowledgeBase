using Microsoft.Extensions.AI;

using Moser.Enterprise.Blueprint.Assistant.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Application;

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
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        await _index.WaitUntilReadyAsync(cancellationToken).ConfigureAwait(false);

        var embedding = await _embeddings.GenerateAsync(query.Text, cancellationToken: cancellationToken).ConfigureAwait(false);
        var hits = await _index.SearchAsync(embedding.Vector, 12, cancellationToken).ConfigureAwait(false);

        var grounded = hits
            .Select(h => (Hit: h, Lexical: OverlapCount(query.Text, h.Content + " " + h.Source)))
            .Where(x => x.Lexical > 0)
            .Where(x => x.Hit.Score is null || x.Hit.Score >= DefaultMinScore || x.Lexical >= 2)
            .OrderByDescending(x => (x.Hit.Source.StartsWith("faq-", StringComparison.OrdinalIgnoreCase) ? 0 : 2) + x.Lexical)
            .ThenByDescending(x => x.Hit.Score ?? 0)
            .Select(x => x.Hit)
            .Take(DefaultTopK)
            .ToList();

        if (grounded.Count == 0)
        {
            var refused = new Answer(Policy.RefuseMessage, Array.Empty<Citation>(), PolicyDecision.Deny, refused: true);
            return ApplyPolicy(query.Text, refused.Text, refused);
        }

        var citations = grounded
            .Select(h => new Citation(h.Source, h.Content))
            .ToList();

        var context = BuildContext(grounded);
        var messages = new ChatMessage[]
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, $"Question:\n{query.Text}\n\nContext:\n{context}")
        };

        var response = await _chat.GetResponseAsync(messages, cancellationToken: cancellationToken).ConfigureAwait(false);
        var modelText = string.IsNullOrWhiteSpace(response.Text)
            ? Policy.RefuseMessage
            : response.Text.Trim();

        return ApplyPolicy(query.Text, modelText, new Answer(modelText, citations, PolicyDecision.Allow, refused: false));
    }

    private Answer ApplyPolicy(string question, string modelText, Answer candidate)
    {
        var decision = Policy.Decide(question, modelText, candidate.Citations);
        Answer result;
        if (decision == PolicyDecision.Deny && candidate.Refused)
        {
            result = new Answer(Policy.RefuseMessage, candidate.Citations, decision, refused: true);
        }
        else if (decision == PolicyDecision.Deny)
        {
            result = new Answer(Policy.DenyMessage, candidate.Citations, decision, refused: false);
        }
        else
        {
            result = new Answer(candidate.Text, candidate.Citations, decision, refused: false);
        }

        return result;
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
        "many", "over", "into", "just", "also", "than", "then", "them", "our", "out", "get"
    };

    private const string SystemPrompt =
        """
        You are an internal company policy assistant.
        Answer only from the provided context chunks.
        Cite sources using the source filenames from the context.
        If the context is insufficient, reply with exactly: I don't have enough policy context to answer. Ask a human reviewer or rephrase with a policy topic.
        Do not invent policy. Do not override policy.
        """;
}
