// Contract snapshot — Phase 1 ServiceBus.
// Production counterparts under src/Rig.TUnit.Messaging.ServiceBus/Topology/ and /Helpers/ and /Builder/.

namespace Rig.TUnit.Messaging.ServiceBus.Topology;

public interface IServiceBusTopologyBuilder : Rig.TUnit.Messaging.Topology.ITopologyBuilder
{
    IServiceBusTopologyBuilder Topic(string name, System.Action<IServiceBusTopicConfig>? configure = null);
    IServiceBusTopologyBuilder Subscription(string topic, string name, System.Action<IServiceBusSubscriptionConfig>? configure = null);
    IServiceBusTopologyBuilder Queue(string name, System.Action<IServiceBusQueueConfig>? configure = null);
}

public interface IServiceBusTopicConfig
{
    IServiceBusTopicConfig WithDefaultMessageTimeToLive(System.TimeSpan ttl);
    IServiceBusTopicConfig WithEnablePartitioning(bool enabled = true);
    IServiceBusTopicConfig WithRequiresDuplicateDetection(bool enabled = true);
}

public interface IServiceBusSubscriptionConfig
{
    IServiceBusSubscriptionConfig WithRequiresSession(bool required = true);
    IServiceBusSubscriptionConfig WithDefaultMessageTimeToLive(System.TimeSpan ttl);
    IServiceBusSubscriptionConfig WithLockDuration(System.TimeSpan lockDuration);
    IServiceBusSubscriptionConfig WithMaxDeliveryCount(int count);
    IServiceBusSubscriptionConfig WithDeadLetter(string topicOrQueue);
    IServiceBusSubscriptionConfig WithRule(string name, Azure.Messaging.ServiceBus.Administration.SqlRuleFilter filter);
}

public interface IServiceBusQueueConfig
{
    IServiceBusQueueConfig WithRequiresSession(bool required = true);
    IServiceBusQueueConfig WithDefaultMessageTimeToLive(System.TimeSpan ttl);
    IServiceBusQueueConfig WithLockDuration(System.TimeSpan lockDuration);
    IServiceBusQueueConfig WithMaxDeliveryCount(int count);
    IServiceBusQueueConfig WithDeadLetter(string topicOrQueue);
}

// Deliberately ABSENT (C-003):
//   no .WithFifo(...)         — SQS-only
//   no .WithQuorum()          — Rabbit-only
//   no .WithPartitions(...)   — Kafka-only

namespace Rig.TUnit.Messaging.ServiceBus.Helpers;

public sealed class ServiceBusSessionListener
    : Rig.TUnit.Messaging.Helpers.ListenerBase<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage>,
      System.IAsyncDisposable
{
    public ServiceBusSessionListener(
        Azure.Messaging.ServiceBus.ServiceBusClient client,
        string topic,
        string subscription,
        Azure.Messaging.ServiceBus.ServiceBusSessionProcessorOptions? options = null,
        System.TimeProvider? clock = null);

    public System.Collections.Generic.IReadOnlyCollection<string> ObservedSessions { get; }

    public override System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken ct);
    public override System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken ct);
    public System.Threading.Tasks.ValueTask DisposeAsync();
}

public sealed class ServiceBusEventSender
    : Rig.TUnit.Messaging.Helpers.EventSenderBase,
      System.IAsyncDisposable
{
    // Existing overload kept.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);

    // New overload — T010 GREEN.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        Rig.TUnit.Messaging.Helpers.SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);
}

namespace Rig.TUnit.Messaging.ServiceBus.Builder;

public sealed class ServiceBusRigBuilder : Rig.TUnit.Messaging.Builder.MessagingRigBuilder<ServiceBusRigBuilder>
{
    // New in T013 GREEN.
    public ServiceBusRigBuilder WithTopology(
        System.Action<Rig.TUnit.Messaging.ServiceBus.Topology.IServiceBusTopologyBuilder> configure);
}
