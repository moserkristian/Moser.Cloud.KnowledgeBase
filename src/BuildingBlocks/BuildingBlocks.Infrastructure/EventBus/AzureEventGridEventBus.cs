using Azure;
using Azure.Messaging.EventGrid;

using Moser.Enterprise.Blueprint.BuildingBlocks.Application.Events;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.BuildingBlocks.Infrastructure.EventBus;

public sealed class AzureEventGridEventBus : IEventBus
{
    private readonly EventGridPublisherClient _client;

    public AzureEventGridEventBus(string endpoint, string accessKey)
        => _client = new EventGridPublisherClient(
            new Uri(endpoint),
            new AzureKeyCredential(accessKey));

    public async Task Publish<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var json = JsonSerializer.Serialize(@event);
        await _client.SendEventAsync(new EventGridEvent(
            subject: @event.GetType().Name,
            eventType: @event.GetType().FullName!,
            dataVersion: "1.0",
            data: BinaryData.FromString(json)), ct);
    }
}
