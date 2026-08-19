using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Moser.Enterprise.Blueprint.Ingestion.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal sealed class SeedIngestionHostedService : IHostedService
{
    private readonly IngestSeedHandler _ingest;
    private readonly AssistantWorkspace _workspace;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedIngestionHostedService> _logger;

    public SeedIngestionHostedService(
        IngestSeedHandler ingest,
        AssistantWorkspace workspace,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<SeedIngestionHostedService> logger)
    {
        _ingest = ingest;
        _workspace = workspace;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var directory = SeedPathResolver.Resolve(_configuration, _environment);
        _logger.LogInformation("Ingesting assistant policy seed from {Directory}", directory ?? "(missing)");
        try
        {
            await _ingest.Handle(new IngestSeed(directory), cancellationToken).ConfigureAwait(false);
            _workspace.MarkIngested();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Policy seed ingest failed. The assistant will start, but the index may be empty until you reset it from /assistant/status.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class SeedPathResolver
{
    public static string? Resolve(IConfiguration configuration, IHostEnvironment? environment)
        => Resolve(configuration[$"{AssistantOptions.SectionName}:SeedPath"] ?? "data/seed/policy", environment);

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
