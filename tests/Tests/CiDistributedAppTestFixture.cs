using Aspire.Hosting;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using AppHost = Moser.Enterprise.Blueprint.AppHost;

namespace Moser.Enterprise.Blueprint.Tests;

public sealed class CiDistributedAppTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public HttpClient? HttpClient { get; private set; }

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        builder.Services.Configure<DistributedApplicationOptions>(options =>
        {
            options.DisableDashboard = true;
        });

        _app = await builder.BuildAsync();
        var notifications = _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync().WaitAsync(TimeSpan.FromMinutes(3));
        await notifications
            .WaitForResourceAsync(AppHost.Program.WebFrontendName, KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(3));

        HttpClient = _app.CreateHttpClient(AppHost.Program.WebFrontendName);
        HttpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
