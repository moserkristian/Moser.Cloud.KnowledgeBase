using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Moser.RagAi.Assistant.Application;
using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Infrastructure;

internal sealed class AssistantWorkspace : IAssistantWorkspace
{
    private readonly AssistantRuntimeInfo _runtime;
    private readonly IDocumentIndex _index;
    private readonly ISourceLibrary _sources;
    private readonly IngestSeedHandler _ingest;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly AssistantOptions _options;
    private readonly object _gate = new();
    private RagScenario _scenario;
    private DateTimeOffset? _lastIngestUtc;

    public AssistantWorkspace(
        AssistantRuntimeInfo runtime,
        IDocumentIndex index,
        ISourceLibrary sources,
        IngestSeedHandler ingest,
        IConfiguration configuration,
        IHostEnvironment environment,
        IOptions<AssistantOptions> options)
    {
        _runtime = runtime;
        _index = index;
        _sources = sources;
        _ingest = ingest;
        _configuration = configuration;
        _environment = environment;
        _options = options.Value;
        _scenario = SeedPathResolver.ResolveInitialScenario(_configuration, _options);
    }

    public RagScenario CurrentScenario
    {
        get
        {
            lock (_gate)
            {
                return _scenario;
            }
        }
    }

    public IReadOnlyList<RagScenarioInfo> ListScenarios() => RagScenarios.All;

    public IReadOnlyList<SourceDocument> ListSources() => _sources.Current;

    public string? ResolveSourcePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var safe = Path.GetFileName(fileName);
        foreach (var document in _sources.Current)
        {
            if (string.Equals(document.FileName, safe, StringComparison.OrdinalIgnoreCase)
                && File.Exists(document.FullPath))
            {
                return document.FullPath;
            }
        }

        return null;
    }

    public void MarkIngested() => _lastIngestUtc = DateTimeOffset.UtcNow;

    public async Task<AssistantStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = _runtime.Endpoint;
        var reachable = !string.IsNullOrWhiteSpace(endpoint)
            && await OllamaGateway.ProbeAsync(endpoint, cancellationToken).ConfigureAwait(false);
        var scenario = CurrentScenario;
        var info = RagScenarios.Get(scenario);

        return new AssistantStatus(
            _runtime.Provider,
            _runtime.Model,
            _runtime.EmbeddingModel,
            _runtime.IsStub,
            endpoint,
            reachable,
            await _index.CountAsync(cancellationToken).ConfigureAwait(false),
            _lastIngestUtc,
            scenario,
            info.Title,
            _index.Provider);
    }

    public async Task<IReadOnlyList<IndexRow>> ListIndexAsync(CancellationToken cancellationToken = default)
    {
        var chunks = await _index.ListAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<IndexRow>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var span = chunk.Embedding.Span;
            var take = Math.Min(8, span.Length);
            var preview = new float[take];
            span[..take].CopyTo(preview);
            rows.Add(new IndexRow(chunk.Id, chunk.Source, chunk.Content, span.Length, preview));
        }

        return rows;
    }

    public Task ResetGeneratedDataAsync(CancellationToken cancellationToken = default)
        => IngestCurrentAsync(cancellationToken);

    public async Task SwitchScenarioAsync(RagScenario scenario, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(scenario))
        {
            scenario = RagScenarios.Default;
        }

        lock (_gate)
        {
            _scenario = scenario;
        }

        await IngestCurrentAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task IngestCurrentAsync(CancellationToken cancellationToken)
    {
        _index.BeginReset();
        await _index.ClearAsync(cancellationToken).ConfigureAwait(false);
        var directory = SeedPathResolver.ResolveForScenario(CurrentScenario, _options, _environment);
        await _ingest.Handle(new IngestSeed(directory), cancellationToken).ConfigureAwait(false);
        MarkIngested();
    }
}
