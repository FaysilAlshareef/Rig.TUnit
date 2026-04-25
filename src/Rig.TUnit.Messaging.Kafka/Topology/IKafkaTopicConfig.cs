namespace Rig.TUnit.Messaging.Kafka.Topology;

/// <summary>
/// Fluent configuration for a Kafka topic declaration inside <see cref="IKafkaTopologyBuilder"/>.
/// </summary>
public interface IKafkaTopicConfig
{
    IKafkaTopicConfig WithPartitions(int count);
    IKafkaTopicConfig WithReplicationFactor(short factor);
    IKafkaTopicConfig WithConfig(string key, string value);
}
