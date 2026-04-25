using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.RabbitMq.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Builder;

public sealed class RabbitMqRigBuilder : MessagingRigBuilder<RabbitMqRigBuilder>
{
    private RabbitMqTopologyBuilder? _topology;

    public RabbitMqRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;

    public RabbitMqRigBuilder WithTopology(Action<IRabbitMqTopologyBuilder> configure)
    {
        _topology ??= new RabbitMqTopologyBuilder(ConnectionString);
        configure(_topology);
        return this;
    }

    public Task ApplyTopologyAsync(CancellationToken ct = default)
    {
        if (_topology is null)
            return Task.CompletedTask;
        return _topology.ApplyAsync(ct);
    }
}
