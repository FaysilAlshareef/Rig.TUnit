using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Kafka.Topology;

namespace Rig.TUnit.Messaging.Kafka.Builder;

public sealed class KafkaRigBuilder : MessagingRigBuilder<KafkaRigBuilder>
{
    private KafkaTopologyBuilder? _topology;

    public KafkaRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;

    /// <summary>
    /// Records Kafka topology declarations to be applied by <see cref="ApplyTopologyAsync"/>.
    /// Returns <c>this</c> for fluent chaining.
    /// </summary>
    public KafkaRigBuilder WithTopology(Action<IKafkaTopologyBuilder> configure)
    {
        _topology ??= new KafkaTopologyBuilder(ConnectionString);
        configure(_topology);
        return this;
    }

    /// <summary>
    /// Applies all recorded topology declarations to the Kafka broker, idempotently.
    /// </summary>
    public Task ApplyTopologyAsync(CancellationToken ct = default)
    {
        if (_topology is null)
            return Task.CompletedTask;
        return _topology.ApplyAsync(ct);
    }
}
