using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Contracts;

/// <summary>Contract implemented by every messaging fixture (ServiceBus, Kafka, RabbitMQ, SQS, NATS).</summary>
public interface IMessagingRig : IRigConnectionSource
{
    IsolationKey IsolationKey { get; }

    /// <summary>Per-test topic/queue name suffix.</summary>
    string TopicName { get; }
}
