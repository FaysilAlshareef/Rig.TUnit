// Contract snapshot — Phase 4 RabbitMQ.
// Production counterparts under src/Rig.TUnit.Messaging.RabbitMq/Topology/, /Helpers/, /Builder/.

namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public enum ExchangeType
{
    Direct,
    Topic,
    Fanout,
    Headers,
}

public interface IRabbitMqTopologyBuilder : Rig.TUnit.Messaging.Topology.ITopologyBuilder
{
    IRabbitMqTopologyBuilder Exchange(string name, ExchangeType type, System.Action<IRabbitMqExchangeConfig>? configure = null);
    IRabbitMqTopologyBuilder Queue(string name, System.Action<IRabbitMqQueueConfig>? configure = null);
    IRabbitMqTopologyBuilder Binding(string exchange, string queue, string routingKey);
}

// Deliberately ABSENT (C-003):
//   no .Subscription(...)    — ServiceBus-only
//   no .Stream(...)          — NATS-only
//   no .Topic(...) on the builder (topic exchange is created via .Exchange(name, ExchangeType.Topic))

public interface IRabbitMqExchangeConfig
{
    IRabbitMqExchangeConfig Durable(bool durable = true);
    IRabbitMqExchangeConfig AutoDelete(bool autoDelete = false);
}

public interface IRabbitMqQueueConfig
{
    IRabbitMqQueueConfig Durable(bool durable = true);
    IRabbitMqQueueConfig WithMessageTtl(System.TimeSpan ttl);
    IRabbitMqQueueConfig WithMaxLength(int count);
    IRabbitMqQueueConfig WithMaxPriority(byte max);
    IRabbitMqQueueConfig WithDeadLetterExchange(string exchange, string? routingKey = null);
    IRabbitMqQueueConfig WithQuorum();
}

// Deliberately ABSENT on IRabbitMqQueueConfig (C-003):
//   no .WithFifo(...)         — SQS-only
//   no .WithRequiresSession   — ServiceBus-only
//   no .WithPartitions(...)   — Kafka-only

namespace Rig.TUnit.Messaging.RabbitMq.Helpers;

public sealed class RabbitMqEventSender : Rig.TUnit.Messaging.Helpers.EventSenderBase, System.IAsyncDisposable
{
    // Existing overload kept.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);

    /// <summary>
    /// T040 GREEN: explicit exchange + routingKey; PartitionKey written to x-partition-key header
    /// so listener can recover the key after broker strips routing key before delivery.
    /// </summary>
    public System.Threading.Tasks.Task SendAsync(
        string body,
        Rig.TUnit.Messaging.Helpers.SendContext context,
        string? exchange = null,
        string? routingKey = null,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);
}

namespace Rig.TUnit.Messaging.RabbitMq.Builder;

public sealed class RabbitMqRigBuilder : Rig.TUnit.Messaging.Builder.MessagingRigBuilder<RabbitMqRigBuilder>
{
    // New in T042 GREEN.
    public RabbitMqRigBuilder WithTopology(
        System.Action<Rig.TUnit.Messaging.RabbitMq.Topology.IRabbitMqTopologyBuilder> configure);
}
