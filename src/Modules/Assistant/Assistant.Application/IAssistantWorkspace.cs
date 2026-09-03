using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Moser.RagAi.Ingestion.Application;

namespace Moser.RagAi.Assistant.Application;

public sealed record AssistantStatus(
    string Provider,
    string Model,
    string EmbeddingModel,
    bool IsStub,
    string? LlmEndpoint,
    bool LlmReachable,
    int ChunkCount,
    DateTimeOffset? LastIngestUtc,
    RagScenario Scenario,
    string ScenarioTitle,
    string VectorStore);

public sealed record IndexRow(
    string Id,
    string Source,
    string Content,
    int Dimensions,
    IReadOnlyList<float> VectorPreview);

public interface IAssistantWorkspace
{
    RagScenario CurrentScenario { get; }

    IReadOnlyList<RagScenarioInfo> ListScenarios();

    IReadOnlyList<SourceDocument> ListSources();

    string? ResolveSourcePath(string fileName);

    Task<AssistantStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IndexRow>> ListIndexAsync(CancellationToken cancellationToken = default);

    Task ResetGeneratedDataAsync(CancellationToken cancellationToken = default);

    Task SwitchScenarioAsync(RagScenario scenario, CancellationToken cancellationToken = default);
}
