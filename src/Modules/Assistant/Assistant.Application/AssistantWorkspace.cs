using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Application;

public sealed record AssistantStatus(
    string Provider,
    string Model,
    string EmbeddingModel,
    bool IsStub,
    string? LlmEndpoint,
    bool LlmReachable,
    int ChunkCount,
    DateTimeOffset? LastIngestUtc);

public interface IAssistantWorkspace
{
    Task<AssistantStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task ResetGeneratedDataAsync(CancellationToken cancellationToken = default);
}
