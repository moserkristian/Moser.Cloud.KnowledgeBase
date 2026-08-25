using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Application;

public sealed record AssistantStatus(
    string Provider,
    string Model,
    string EmbeddingModel,
    bool IsStub,
    string? LlmEndpoint,
    bool LlmReachable,
    int ChunkCount,
    DateTimeOffset? LastIngestUtc);

public sealed record IndexRow(
    string Id,
    string Source,
    string Content,
    int Dimensions,
    IReadOnlyList<float> VectorPreview);

public interface IAssistantWorkspace
{
    Task<AssistantStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndexRow>> ListIndexAsync(CancellationToken cancellationToken = default);
    Task ResetGeneratedDataAsync(CancellationToken cancellationToken = default);
}
