namespace Rig.TUnit.Messaging.Sqs.Topology;

/// <summary>
/// Fluent configuration for an SQS queue declaration inside <see cref="ISqsTopologyBuilder"/>.
/// </summary>
public interface ISqsQueueConfig
{
    ISqsQueueConfig WithFifo(bool contentBasedDeduplication = false);
    ISqsQueueConfig WithVisibilityTimeout(TimeSpan timeout);
    ISqsQueueConfig WithMessageRetentionPeriod(TimeSpan period);
    ISqsQueueConfig WithDeadLetter(string queue, int maxReceiveCount);
}
