namespace Moser.RagAi.Assistant.Infrastructure;

public sealed record AssistantRuntimeInfo(
    string Provider,
    string Model,
    string EmbeddingModel,
    bool IsStub,
    string? Endpoint);
