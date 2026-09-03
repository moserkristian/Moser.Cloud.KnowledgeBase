using Moser.RagAi.Assistant.Domain;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Application;

public sealed record AskQuestion(string Text);

public enum AskStage
{
    Retrieving,
    Generating,
    CheckingPolicy,
    Done
}

public sealed record AskUpdate(
    AskStage Stage,
    string? Text,
    IReadOnlyList<Citation> Citations,
    Answer? Completed,
    string? Status = null);

public interface IAskQuestion
{
    Task<Answer> Handle(AskQuestion query, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AskUpdate> StreamAsync(AskQuestion query, CancellationToken cancellationToken = default);
}
