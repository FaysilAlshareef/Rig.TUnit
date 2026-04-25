using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public interface IRabbitMqTopologyBuilder : ITopologyBuilder
{
    IRabbitMqExchangeConfig Exchange(string name, ExchangeType type, Action<IRabbitMqExchangeConfig>? configure = null);
    IRabbitMqTopologyBuilder Queue(string name, Action<IRabbitMqQueueConfig>? configure = null);
}
