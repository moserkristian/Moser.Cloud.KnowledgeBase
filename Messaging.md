# Messaging Architecture

> uses a robust “outbox” 
pattern to guarantee consistency between domain changes and event publishing. 
In this pattern, when a use case or command handler executes,
all domain changes and integration events are saved in the same database transaction.
This is typically done by storing the integration events
in a dedicated log table (often called the IntegrationEventLog) alongside the domain changes.
Once the transaction is committed,
a separate background service (or publisher)
reads the pending integration events from the log and
publishes them (for example, to Azure Service Bus/EventHub or RabbitMQ).
After successful publishing,
the events are marked as published or removed from the log.

Why this is safe and resilient:

Atomicity: Because the domain changes and the integration events 
		   are saved within one transaction, you ensure that either both are committed or neither is. 
		   This eliminates the risk of a “lost event” 
		   if a failure occurs between saving data and publishing the event.
Resiliency: By using EF Core’s execution strategy 
		    (which supports transient fault handling) 
		    and a background publisher that can retry failed 
			publishes, the pattern is resilient to transient errors.
Auditability: Storing integration events in the database allows you to monitor, 
		      reprocess, or even manually intervene if some events fail to publish.
Decoupling: This approach decouples the process 
		    of persisting domain changes from the process 
			of publishing events, allowing each part to be scaled and monitored separately. 
			You can also integrate dashboards 
			(e.g. .NET Aspire or other monitoring tools) to track unprocessed events.