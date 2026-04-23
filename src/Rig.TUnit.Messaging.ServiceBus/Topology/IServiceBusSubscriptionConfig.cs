using Azure.Messaging.ServiceBus.Administration;

namespace Rig.TUnit.Messaging.ServiceBus.Topology;

public interface IServiceBusSubscriptionConfig
{
    IServiceBusSubscriptionConfig WithRequiresSession(bool required = true);
    IServiceBusSubscriptionConfig WithDefaultMessageTimeToLive(TimeSpan ttl);
    IServiceBusSubscriptionConfig WithLockDuration(TimeSpan lockDuration);
    IServiceBusSubscriptionConfig WithMaxDeliveryCount(int count);
    IServiceBusSubscriptionConfig WithRule(string name, SqlRuleFilter filter);
}
