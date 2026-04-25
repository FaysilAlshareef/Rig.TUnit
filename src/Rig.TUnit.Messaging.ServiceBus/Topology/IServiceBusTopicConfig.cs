namespace Rig.TUnit.Messaging.ServiceBus.Topology;

public interface IServiceBusTopicConfig
{
    IServiceBusTopicConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusTopicConfig WithEnablePartitioning(bool enabled = true);
    IServiceBusTopicConfig WithRequiresDuplicateDetection(bool enabled = true);
}
