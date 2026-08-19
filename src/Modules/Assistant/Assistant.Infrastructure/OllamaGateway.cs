using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal static class OllamaGateway
{
    public static bool IsReachable(string endpoint)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var client = CreateClient(TimeSpan.FromSeconds(5));
                using var response = client.GetAsync(Combine(endpoint, "/api/tags")).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Native Windows Ollama binds 127.0.0.1; localhost/IPv6 can fail the first probe.
            }

            if (attempt < 2)
            {
                Thread.Sleep(400);
            }
        }

        return false;
    }

    public static async Task<bool> ProbeAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient(TimeSpan.FromSeconds(5));
            using var response = await client.GetAsync(Combine(endpoint, "/api/tags"), cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static string NormalizeEndpoint(string endpoint)
    {
        var trimmed = PreferIpv4Loopback(endpoint.TrimEnd('/'));
        return trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + "/v1";
    }

    private static HttpClient CreateClient(TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            ConnectTimeout = TimeSpan.FromSeconds(3)
        };
        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout };
    }

    private static string Combine(string endpoint, string path)
    {
        var trimmed = PreferIpv4Loopback(endpoint.TrimEnd('/'));
        if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^3].TrimEnd('/');
        }

        return trimmed + path;
    }

    private static string PreferIpv4Loopback(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        var builder = new UriBuilder(uri) { Host = "127.0.0.1" };
        var path = builder.Path is "/" or "" ? string.Empty : builder.Path.TrimEnd('/');
        return $"{builder.Scheme}://{builder.Host}:{builder.Port}{path}";
    }
}
