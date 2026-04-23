using NATS.Client.JetStream;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Nats.Topology;

namespace Rig.TUnit.Messaging.Nats.Builder;

public sealed class NatsRigBuilder : MessagingRigBuilder<NatsRigBuilder>
{
    private readonly INatsJSContext? _jetStream;
    private NatsTopologyBuilder? _topology;

    public NatsRigBuilder(RigBuilder root, IRigConnectionSource source, INatsJSContext? jetStream = null)
        : base(root, source) { _jetStream = jetStream; }

    public string ConnectionString => Source.ConnectionString;

    public NatsRigBuilder WithTopology(Action<INatsTopologyBuilder> configure)
    {
        if (_jetStream is null)
            throw new InvalidOperationException(
                "WithTopology requires a JetStream context. Use UseNats(NatsJetStreamFixture, ...) instead of UseNats(NatsFixture, ...).");

        _topology ??= new NatsTopologyBuilder(_jetStream);
        configure(_topology);
        return this;
    }

    public Task ApplyTopologyAsync(CancellationToken ct = default)
    {
        if (_topology is null) return Task.CompletedTask;
        return _topology.ApplyAsync(ct);
    }
}
