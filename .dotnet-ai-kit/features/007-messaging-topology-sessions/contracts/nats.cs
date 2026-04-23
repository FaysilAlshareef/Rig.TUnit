// Contract snapshot — Phase 5 NATS JetStream.
// Production counterparts under src/Rig.TUnit.Messaging.Nats/Topology/, /Fixtures/, /Helpers/, /Builder/.

namespace Rig.TUnit.Messaging.Nats.Topology;

public interface INatsTopologyBuilder : Rig.TUnit.Messaging.Topology.ITopologyBuilder
{
    INatsTopologyBuilder Stream(string name, System.Action<INatsStreamConfig>? configure = null);
    INatsTopologyBuilder Consumer(string stream, string name, System.Action<INatsConsumerConfig>? configure = null);
}

// Deliberately ABSENT (C-003):
//   no .Queue(...) / .Topic(...) / .Exchange(...) / .Subscription(...)

public interface INatsStreamConfig
{
    INatsStreamConfig WithSubjects(params string[] subjects);
    INatsStreamConfig WithRetention(NATS.Client.JetStream.Models.RetentionPolicy policy);
    INatsStreamConfig WithMaxMessages(long max);
    INatsStreamConfig WithStorage(NATS.Client.JetStream.Models.StreamConfigStorage storage);
}

public interface INatsConsumerConfig
{
    INatsConsumerConfig WithFilterSubjects(params string[] subjects);
    INatsConsumerConfig WithDeliverPolicy(NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy policy);
    INatsConsumerConfig WithReplayPolicy(NATS.Client.JetStream.Models.ConsumerConfigReplayPolicy policy);

    /// <summary>
    /// Shorthand for DeliverPolicy.All + ReplayPolicy.Instant + FlowControl=true + AckPolicy.Explicit.
    /// Applies the canonical ordered-consumer recipe.
    /// </summary>
    INatsConsumerConfig WithOrderedConsumer();
}

namespace Rig.TUnit.Messaging.Nats.Fixtures;

public sealed class NatsJetStreamFixture : Rig.TUnit.Messaging.Fixtures.MessagingFixtureBase, System.IAsyncDisposable
{
    public string ConnectionString { get; }
    public NATS.Client.JetStream.INatsJSContext JetStream { get; }
}

namespace Rig.TUnit.Messaging.Nats.Helpers;

public sealed class NatsJetStreamEventSender : Rig.TUnit.Messaging.Helpers.EventSenderBase, System.IAsyncDisposable
{
    public NatsJetStreamEventSender(
        NATS.Client.JetStream.INatsJSContext jetStream,
        string subject);

    public System.Threading.Tasks.Task SendAsync(
        string body,
        Rig.TUnit.Messaging.Helpers.SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);
}

public sealed class NatsJetStreamListener
    : Rig.TUnit.Messaging.Helpers.ListenerBase<NATS.Client.JetStream.Models.JSMsg>,
      System.IAsyncDisposable
{
    public NatsJetStreamListener(
        NATS.Client.JetStream.INatsJSContext jetStream,
        string streamName,
        string consumerName,
        System.TimeProvider? clock = null);

    public override System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken ct);
    public override System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken ct);
    public System.Threading.Tasks.ValueTask DisposeAsync();
}

namespace Rig.TUnit.Messaging.Nats.Builder;

public sealed class NatsRigBuilder : Rig.TUnit.Messaging.Builder.MessagingRigBuilder<NatsRigBuilder>
{
    // New in T054 GREEN.
    public NatsRigBuilder WithTopology(
        System.Action<Rig.TUnit.Messaging.Nats.Topology.INatsTopologyBuilder> configure);
}
