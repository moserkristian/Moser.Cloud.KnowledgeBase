using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moser.RagAi.Assistant.Application;
using Moser.RagAi.Ingestion.Application;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Infrastructure;

internal sealed class AssistantWorkspace : IAssistantWorkspace
{
    private readonly AssistantRuntimeInfo _runtime;
    private readonly IDocumentIndex _index;
    private readonly IngestSeedHandler _ingest;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private DateTimeOffset? _lastIngestUtc;

    public AssistantWorkspace(
        AssistantRuntimeInfo runtime,
        IDocumentIndex index,
        IngestSeedHandler ingest,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _runtime = runtime;
        _index = index;
        _ingest = ingest;
        _configuration = configuration;
        _environment = environment;
    }

    public void MarkIngested() => _lastIngestUtc = DateTimeOffset.UtcNow;

    public async Task<AssistantStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = _runtime.Endpoint;
        var reachable = !string.IsNullOrWhiteSpace(endpoint)
            && await OllamaGateway.ProbeAsync(endpoint, cancellationToken).ConfigureAwait(false);

        return new AssistantStatus(
            _runtime.Provider,
            _runtime.Model,
            _runtime.EmbeddingModel,
            _runtime.IsStub,
            endpoint,
            reachable,
            await _index.CountAsync(cancellationToken).ConfigureAwait(false),
            _lastIngestUtc);
    }

    public async Task ResetGeneratedDataAsync(CancellationToken cancellationToken = default)
    {
        _index.BeginReset();
        await _index.ClearAsync(cancellationToken).ConfigureAwait(false);
        var directory = SeedPathResolver.Resolve(_configuration, _environment);
        await _ingest.Handle(new IngestSeed(directory), cancellationToken).ConfigureAwait(false);
        MarkIngested();
    }
}
