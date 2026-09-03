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

    public static PolicyOutcome Decide(
        string question,
        string? modelAnswer,
        IReadOnlyList<Citation> citations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        citations ??= Array.Empty<Citation>();

        if (IsHardDenyQuestion(question, out var hardBlock))
        {
            return new PolicyOutcome(
                PolicyDecision.Deny,
                $"Hard block: {hardBlock}. Model text discarded.");
        }

        if (RetrievedPolicyForbids(question, modelAnswer, citations, out var forbidden))
        {
            return new PolicyOutcome(
                PolicyDecision.Deny,
                $"Retrieved policy forbids this ({forbidden}). Model text replaced.");
        }

        if (citations.Count == 0)
        {
            return new PolicyOutcome(
                PolicyDecision.Deny,
                "Hybrid search kept no overlapping passage. The model did not run.");
        }

        if (TryConflict(question, citations, out var conflict))
        {
            return new PolicyOutcome(
                PolicyDecision.NeedsHuman,
                $"{conflict} Policy will not pick a value.");
        }

        return new PolicyOutcome(
            PolicyDecision.Allow,
            $"{citations.Count} passage(s) retrieved. No conflict. Model text kept.");
    }

    private static bool IsHardDenyQuestion(string question, out string reason)
    {
        var q = question.ToLowerInvariant();
        if (ContainsAny(q,
                "share my password",
                "share the password",
                "share my api key",
                "share api key",
                "share the api key",
                "share credentials",
                "give me the api key"))
        {
            reason = "credentials / secrets";
            return true;
        }

        if (ContainsAny(q,
                "override the refund",
                "override refund policy",
                "ignore the policy",
                "ignore policy",
                "bypass policy"))
        {
            reason = "policy bypass";
            return true;
        }

        if (ContainsAny(q,
                "insider trading",
                "unreleased earnings",
                "material non-public",
                "mnpi"))
        {
            reason = "insider dealing";
            return true;
        }

        if (ContainsAny(q, "dump customer pii", "export customer pii", "share customer pii"))
        {
            reason = "customer PII";
            return true;
        }

        reason = "";
        return false;
    }

    private static bool RetrievedPolicyForbids(
        string question,
        string? modelAnswer,
        IReadOnlyList<Citation> citations,
        out string reason)
    {
        reason = "";
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
            reason = "cash gifts";
            return true;
        }

        if (LooksLikeSecretQuestion(q) &&
            ContainsAny(corpus, "password", "credential", "api key", "secret") &&
            ContainsAny(corpus, "never share", "must not share", "do not share", "prohibited"))
        {
            reason = "credentials / secrets";
            return true;
        }

        if (LooksLikeInsiderQuestion(q) &&
            ContainsAny(corpus, "insider", "mnpi", "material non-public") &&
            ContainsAny(corpus, "must not", "prohibited", "never"))
        {
            reason = "insider dealing";
            return true;
        }

        if (modelSaidYes && ContainsAny(corpus, "prohibited", "must not", "never share", "not allowed") &&
            TopicAlignsWithProhibition(q, corpus))
        {
            reason = "retrieved prohibition";
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

    private static bool TryConflict(string question, IReadOnlyList<Citation> citations, out string reason)
    {
        var q = question.ToLowerInvariant();

        if (ContainsAny(q, "refund", "return", "rma"))
        {
            var values = DistinctValues(citations, DaysPattern);
            if (values.Count > 1)
            {
                reason = $"Refund windows collide ({JoinNumbers(values)} days).";
                return true;
            }
        }

        if (ContainsAny(q, "expense", "receipt", "reimburse"))
        {
            var values = DistinctValues(citations, MoneyPattern);
            if (values.Count > 1)
            {
                reason = $"Euro caps collide (EUR {JoinNumbers(values)}).";
                return true;
            }
        }

        if (ContainsAny(q, "annual leave", "dovolenka", "leave days")
            && !ContainsAny(q, "bratislava", "office", "nitra", "plant", "závod"))
        {
            var values = DistinctValues(citations, DaysPattern);
            if (values.Count > 1)
            {
                reason = $"Leave entitlements collide ({JoinNumbers(values)} days).";
                return true;
            }
        }

        reason = "";
        return false;
    }

    private static string JoinNumbers(IReadOnlyCollection<int> values)
        => string.Join(", ", values.OrderBy(v => v));

    private static HashSet<int> DistinctValues(IReadOnlyList<Citation> citations, Regex pattern)
    {
        var values = new HashSet<int>();
        foreach (var citation in citations)
        {
            foreach (Match match in pattern.Matches(citation.Chunk))
            {
                if (TryReadNumber(match, out var value))
                {
                    values.Add(value);
                }
            }
        }

        return values;
    }

    /// <summary>
    /// Reads the amount from whichever capture group matched, so a pattern can
    /// accept both prefixed (<c>EUR 25</c>) and suffixed (<c>25 EUR</c>) forms.
    /// </summary>
    private static bool TryReadNumber(Match match, out int value)
    {
        for (var i = 1; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            if (!group.Success)
            {
                continue;
            }

            var digits = group.Value.Replace(",", string.Empty).Replace(" ", string.Empty);
            if (int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static bool ContainsAny(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.Ordinal));

    private static readonly Regex DaysPattern = new(@"(\d+)\s*days?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Euro and dollar caps, written either before or after the amount.</summary>
    private static readonly Regex MoneyPattern = new(
        @"(?:[$€]|\bEUR\b)\s*(\d+(?:[ ,]\d{3})*)|(\d+(?:[ ,]\d{3})*)\s*(?:€|\bEUR\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
}
