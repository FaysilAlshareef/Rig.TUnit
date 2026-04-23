namespace Rig.TUnit.Messaging.ServiceBus.Topology;

/// <summary>
/// Fluent configuration for a Service Bus queue declaration.
/// Per C-003: MUST NOT declare WithFifo, WithQuorum, WithPartitions, or WithSubjects
/// — those belong to Kafka, RabbitMQ, NATS, and SQS providers respectively.
/// </summary>
public interface IServiceBusQueueConfig
{
    IServiceBusQueueConfig WithRequiresSession(bool required = true);
    IServiceBusQueueConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusQueueConfig WithLockDuration(TimeSpan lockDuration);
    IServiceBusQueueConfig WithMaxDeliveryCount(int count);
}
