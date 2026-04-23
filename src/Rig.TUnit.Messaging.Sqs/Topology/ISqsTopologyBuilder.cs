using Rig.TUnit.Messaging.Topology;

namespace Rig.TUnit.Messaging.Sqs.Topology;

/// <summary>
/// SQS-specific topology builder. Only <see cref="Queue"/> is declared — no Topic, Exchange,
/// Stream, or Subscription (those belong to other providers). Per C-003.
/// </summary>
public interface ISqsTopologyBuilder : ITopologyBuilder
{
    ISqsTopologyBuilder Queue(string name, Action<ISqsQueueConfig>? configure = null);
}
