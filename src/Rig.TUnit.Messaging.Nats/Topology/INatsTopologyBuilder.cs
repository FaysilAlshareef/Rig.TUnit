using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.Nats.Topology;

public interface INatsTopologyBuilder : ITopologyBuilder
{
    INatsTopologyBuilder Stream(string name, Action<INatsStreamConfig>? configure = null);
}
