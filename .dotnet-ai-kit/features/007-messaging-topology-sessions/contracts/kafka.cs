// Contract snapshot — Phase 2 Kafka.
// Production counterparts under src/Rig.TUnit.Messaging.Kafka/Topology/, /Options/, /Helpers/, /Builder/.

namespace Rig.TUnit.Messaging.Kafka.Topology;

public interface IKafkaTopologyBuilder : Rig.TUnit.Messaging.Topology.ITopologyBuilder
{
    IKafkaTopologyBuilder Topic(string name, System.Action<IKafkaTopicConfig>? configure = null);
}

// Deliberately ABSENT (C-003):
//   no .Queue(...)           — Kafka has no queues
//   no .Exchange(...)        — Rabbit-only
//   no .Subscription(...)    — ServiceBus-only
//   no .Stream(...)          — NATS-only

public interface IKafkaTopicConfig
{
    IKafkaTopicConfig WithPartitions(int count);                   // default 1
    IKafkaTopicConfig WithReplicationFactor(short factor);         // default 1
    IKafkaTopicConfig WithConfig(string key, string value);        // retention.ms / cleanup.policy / …
}

namespace Rig.TUnit.Messaging.Kafka.Options;

public sealed class KafkaFixtureOptions
{
    public const string SectionName = "Kafka";

    // New in T021 GREEN.
    [System.ComponentModel.DataAnnotations.Range(1, 200)]
    public int DefaultPartitions { get; init; } = 1;

    // … existing options (unchanged).
}

namespace Rig.TUnit.Messaging.Kafka.Helpers;

public sealed class KafkaEventSender : Rig.TUnit.Messaging.Helpers.EventSenderBase, System.IAsyncDisposable
{
    // Existing overload kept.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);

    // New in T020 GREEN. Message.Key = ctx.PartitionKey ?? ctx.SessionKey ?? correlationId ?? Guid.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        Rig.TUnit.Messaging.Helpers.SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);
}

public sealed class KafkaListener
    : Rig.TUnit.Messaging.Helpers.ListenerBase<Confluent.Kafka.ConsumeResult<string, string>>,
      System.IAsyncDisposable
{
    // New in T024 GREEN — optional pinned-partition helper.
    public void Assign(int partition);
}

namespace Rig.TUnit.Messaging.Kafka.Builder;

public sealed class KafkaRigBuilder : Rig.TUnit.Messaging.Builder.MessagingRigBuilder<KafkaRigBuilder>
{
    // New in T023 GREEN.
    public KafkaRigBuilder WithTopology(
        System.Action<Rig.TUnit.Messaging.Kafka.Topology.IKafkaTopologyBuilder> configure);
}
