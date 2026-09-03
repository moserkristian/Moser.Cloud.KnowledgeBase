using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moser.RagAi.Assistant.Application;
using Moser.RagAi.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Assistant.Infrastructure;

internal sealed class SeedIngestionHostedService : IHostedService
{
    private readonly IngestSeedHandler _ingest;
    private readonly AssistantWorkspace _workspace;
    private readonly IOptions<AssistantOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedIngestionHostedService> _logger;

    public SeedIngestionHostedService(
        IngestSeedHandler ingest,
        AssistantWorkspace workspace,
        IOptions<AssistantOptions> options,
        IHostEnvironment environment,
        ILogger<SeedIngestionHostedService> logger)
    {
        _ingest = ingest;
        _workspace = workspace;
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scenario = _workspace.CurrentScenario;
        var directory = SeedPathResolver.ResolveForScenario(scenario, _options.Value, _environment);
        _logger.LogInformation(
            "Ingesting RAG scenario {Scenario} from {Directory}",
            scenario,
            directory ?? "(missing)");
        try
        {
            await _ingest.Handle(new IngestSeed(directory), cancellationToken).ConfigureAwait(false);
            _workspace.MarkIngested();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Seed ingest failed for {Scenario}. The assistant will start, but the index may be empty until you switch/reload from /ask or /status.",
                scenario);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class SeedPathResolver
{
    public static RagScenario ResolveInitialScenario(IConfiguration configuration, AssistantOptions options)
    {
        var raw = configuration[$"{AssistantOptions.SectionName}:Scenario"] ?? options.Scenario;
        if (RagScenarios.TryParse(raw, out var scenario))
        {
            return scenario;
        }

        // Backward compat: SeedPath ending with a known folder name.
        var seedPath = configuration[$"{AssistantOptions.SectionName}:SeedPath"] ?? options.SeedPath;
        if (!string.IsNullOrWhiteSpace(seedPath))
        {
            var name = Path.GetFileName(seedPath.TrimEnd('/', '\\'));
            if (RagScenarios.TryParse(name, out scenario))
            {
                return scenario;
            }
        }

        return RagScenarios.Default;
    }

    public static string? ResolveForScenario(
        RagScenario scenario,
        AssistantOptions options,
        IHostEnvironment? environment)
    {
        var root = string.IsNullOrWhiteSpace(options.SeedRoot) ? "data/seed" : options.SeedRoot;
        var relative = RagScenarios.RelativeSeedPath(scenario, root);
        return Resolve(relative, environment);
    }

    public static string? Resolve(string path, IHostEnvironment? environment)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Path.IsPathRooted(path) && Directory.Exists(path))
        {
            return path;
        }

        foreach (var root in CandidateRoots(environment))
        {
            var combined = Path.GetFullPath(Path.Combine(root, path));
            if (Directory.Exists(combined))
            {
                return combined;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateRoots(IHostEnvironment? environment)
    {
        if (environment is not null)
        {
            yield return environment.ContentRootPath;
        }

        yield return AppContext.BaseDirectory;

        var current = environment?.ContentRootPath ?? AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
        {
            yield return current;
            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }
    }
}
