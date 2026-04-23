using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.Kafka.Topology;

/// <summary>
/// Kafka-specific topology builder. Only <see cref="Topic"/> is declared — no Queue, Exchange,
/// or Subscription (those belong to other providers). Per C-003.
/// </summary>
public interface IKafkaTopologyBuilder : ITopologyBuilder
{
    IKafkaTopologyBuilder Topic(string name, Action<IKafkaTopicConfig>? configure = null);
}
