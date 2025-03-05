+ Root
    + src
        + Microservice_1
            + Infrastructure
                + Identity
                + Messaging
                    + <IntegrationEventServiceName>
                        + <IntegrationEventServiceName>Client.cs
                        + IntegrationEventPublisher.cs
                        + SubscriptionManager.cs
                + Persistence
                    + Configurations
                        + <AggregateRootName>Configuration.cs
                    + Migrations
                    + Repositories
                        + <AggregateRootName>Repository.cs
                    + ApplicationDbContext.cs

## Infrastructure/Messaging/<IntegrationEventServiceName>/IntegrationEventPublisher.cs

```csharp
public class IntegrationEventPublisher : IIntegrationEventService, IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(string connectionString, string topicName, ILogger<IntegrationEventPublisher> logger)
    {
        _logger = logger;
        _serviceBusClient = new ServiceBusClient(connectionString);
        _sender = _serviceBusClient.CreateSender(topicName);
    }

    public async Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        try
        {
            string messageBody = JsonSerializer.Serialize(@event);
            ServiceBusMessage message = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = typeof(TEvent).Name // e.g. "<AggregateRootName><FactName>IntegrationEvent"
            };

            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Published integration event: {EventType}", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing integration event: {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null)
        {
            await _sender.DisposeAsync();
        }
        if (_serviceBusClient is not null)
        {
            await _serviceBusClient.DisposeAsync();
        }
    }
}
```