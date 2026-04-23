using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.ServiceBus.Topology;

/// <summary>
/// Fluent builder for declaring Azure Service Bus topology (topics, subscriptions, queues).
/// Per C-003: fluent methods live here, not on <c>ITopologyBuilder</c> base.
/// Call <see cref="ITopologyBuilder.ApplyAsync"/> to materialise all declarations.
/// </summary>
public interface IServiceBusTopologyBuilder : ITopologyBuilder
{
    IServiceBusTopologyBuilder Topic(string name, Action<IServiceBusTopicConfig>? configure = null);
    IServiceBusTopologyBuilder Subscription(string topic, string name, Action<IServiceBusSubscriptionConfig>? configure = null);
    IServiceBusTopologyBuilder Queue(string name, Action<IServiceBusQueueConfig>? configure = null);
}
