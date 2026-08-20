using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Moser.RagAi.Assistant.Domain;

public static class Policy
{
    public const string RefuseMessage =
        "I don't have enough policy context to answer. Ask a human reviewer or rephrase with a policy topic.";

    public const string DenyMessage =
        "This request is not allowed under company policy. I cannot approve or instruct it.";

    public static PolicyDecision Decide(
        string question,
        string? modelAnswer,
        IReadOnlyList<Citation> citations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        citations ??= Array.Empty<Citation>();

        if (IsHardDenyQuestion(question))
        {
            return PolicyDecision.Deny;
        }

        if (RetrievedPolicyForbids(question, modelAnswer, citations))
        {
            return PolicyDecision.Deny;
        }

        if (citations.Count == 0)
        {
            return PolicyDecision.Deny;
        }

        if (HasConflictingGuidance(question, citations))
        {
            return PolicyDecision.NeedsHuman;
        }

        return PolicyDecision.Allow;
    }

    private static bool IsHardDenyQuestion(string question)
    {
        var q = question.ToLowerInvariant();
        return ContainsAny(q,
            "share my password",
            "share the password",
            "share my api key",
            "share api key",
            "share the api key",
            "share credentials",
            "give me the api key",
            "override the refund",
            "override refund policy",
            "ignore the policy",
            "ignore policy",
            "bypass policy",
            "insider trading",
            "unreleased earnings",
            "material non-public",
            "mnpi",
            "dump customer pii",
            "export customer pii",
            "share customer pii");
    }

    private static bool RetrievedPolicyForbids(
        string question,
        string? modelAnswer,
        IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return false;
        }

        var q = question.ToLowerInvariant();
        var answer = (modelAnswer ?? string.Empty).ToLowerInvariant();
        var corpus = string.Join('\n', citations.Select(c => c.Chunk)).ToLowerInvariant();

        var modelSaidYes = ContainsAny(answer, "yes", "you may", "allowed", "permitted", "you can");

        if (LooksLikeGiftQuestion(q) && ContainsAny(corpus, "cash gift", "cash gifts") &&
            ContainsAny(corpus, "prohibited", "must not", "not allowed", "never"))
        {
            return true;
        }

        if (LooksLikeSecretQuestion(q) &&
            ContainsAny(corpus, "password", "credential", "api key", "secret") &&
            ContainsAny(corpus, "never share", "must not share", "do not share", "prohibited"))
        {
            return true;
        }

        if (LooksLikeInsiderQuestion(q) &&
            ContainsAny(corpus, "insider", "mnpi", "material non-public") &&
            ContainsAny(corpus, "must not", "prohibited", "never"))
        {
            return true;
        }

        if (modelSaidYes && ContainsAny(corpus, "prohibited", "must not", "never share", "not allowed") &&
            TopicAlignsWithProhibition(q, corpus))
        {
            return true;
        }

        return false;
    }

    private static bool TopicAlignsWithProhibition(string question, string corpus)
    {
        if (LooksLikeGiftQuestion(question) && corpus.Contains("gift", StringComparison.Ordinal))
        {
            return true;
        }

        if (LooksLikeSecretQuestion(question) &&
            ContainsAny(corpus, "password", "credential", "api key", "secret"))
        {
            return true;
        }

        if (LooksLikeInsiderQuestion(question) && ContainsAny(corpus, "insider", "mnpi", "trading"))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeGiftQuestion(string q)
        => ContainsAny(q, "cash gift", "gift from a supplier", "gift from a vendor", "hospitality");

    private static bool LooksLikeSecretQuestion(string q)
        => ContainsAny(q, "share", "give me", "send them", "paste") &&
           ContainsAny(q, "password", "api key", "credential", "secret");

    private static bool LooksLikeInsiderQuestion(string q)
        => ContainsAny(q, "insider", "unreleased earnings", "mnpi", "material non-public", "trade on");

    private static bool HasConflictingGuidance(string question, IReadOnlyList<Citation> citations)
    {
        var q = question.ToLowerInvariant();

        if (ContainsAny(q, "refund", "return", "rma"))
        {
            return DistinctValues(citations, DaysPattern).Count > 1;
        }

        if (ContainsAny(q, "expense", "receipt", "reimburse"))
        {
            return DistinctValues(citations, MoneyPattern).Count > 1;
        }

        if (ContainsAny(q, "pto", "annual leave") && !ContainsAny(q, " us ", "us employees", "eu "))
        {
            return DistinctValues(citations, DaysPattern).Count > 1;
        }

        return false;
    }

    private static HashSet<int> DistinctValues(IReadOnlyList<Citation> citations, Regex pattern)
    {
        var values = new HashSet<int>();
        foreach (var citation in citations)
        {
            foreach (Match match in pattern.Matches(citation.Chunk))
            {
                values.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }
        }

        return values;
    }

    private static bool ContainsAny(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.Ordinal));

    private static readonly Regex DaysPattern = new(@"(\d+)\s*days?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MoneyPattern = new(@"\$(\d+)", RegexOptions.Compiled);
}
