using Moser.RagAi.Assistant.Domain;

using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Application;

public sealed record AskQuestion(string Text);

public interface IAskQuestion
{
    Task<Answer> Handle(AskQuestion query, CancellationToken cancellationToken = default);
}
