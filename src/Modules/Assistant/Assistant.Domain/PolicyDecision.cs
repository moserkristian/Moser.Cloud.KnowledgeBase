namespace Moser.RagAi.Assistant.Domain;

public enum PolicyDecision
{
    Allow = 0,
    Deny = 1,
    NeedsHuman = 2
}

public sealed record PolicyOutcome(PolicyDecision Decision, string Reason);
