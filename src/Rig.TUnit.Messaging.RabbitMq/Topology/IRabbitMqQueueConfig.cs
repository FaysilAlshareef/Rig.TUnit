namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public interface IRabbitMqQueueConfig
{
    IRabbitMqQueueConfig WithMessageTtl(TimeSpan ttl);
    IRabbitMqQueueConfig WithMaxLength(int maxLength);
    IRabbitMqQueueConfig WithMaxPriority(int maxPriority);
    IRabbitMqQueueConfig WithDeadLetterExchange(string exchange, string? routingKey = null);
    IRabbitMqQueueConfig WithQuorum();
}
