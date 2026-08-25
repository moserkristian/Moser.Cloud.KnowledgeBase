using System;
using System.Collections.Generic;

namespace Moser.RagAi.Assistant.Domain;

public sealed record Answer
{
    public Answer(
        string text,
        IReadOnlyList<Citation> citations,
        PolicyDecision decision,
        bool refused)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Citations = citations ?? throw new ArgumentNullException(nameof(citations));
        Decision = decision;
        Refused = refused;
    }

    public string Text { get; }
    public IReadOnlyList<Citation> Citations { get; }
    public PolicyDecision Decision { get; }
    public bool Refused { get; }
}
