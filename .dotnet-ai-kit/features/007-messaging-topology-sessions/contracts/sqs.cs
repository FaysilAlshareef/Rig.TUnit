// Contract snapshot — Phase 3 SQS.
// Production counterparts under src/Rig.TUnit.Messaging.Sqs/Topology/, /Helpers/, /Builder/.

namespace Rig.TUnit.Messaging.Sqs.Topology;

public interface ISqsTopologyBuilder : Rig.TUnit.Messaging.Topology.ITopologyBuilder
{
    ISqsTopologyBuilder Queue(string name, System.Action<ISqsQueueConfig>? configure = null);
}

// Deliberately ABSENT (C-003):
//   no .Topic(...)           — SNS integration out of scope
//   no .Exchange / .Stream / .Subscription

public interface ISqsQueueConfig
{
    /// <summary>
    /// Make the queue FIFO. Appends ".fifo" suffix to the name if missing (per AWS rule).
    /// </summary>
    ISqsQueueConfig WithFifo(bool contentBasedDeduplication = false);

    ISqsQueueConfig WithVisibilityTimeout(System.TimeSpan timeout);
    ISqsQueueConfig WithMessageRetentionPeriod(System.TimeSpan period);
    ISqsQueueConfig WithDeadLetter(string queue, int maxReceiveCount);
}

namespace Rig.TUnit.Messaging.Sqs.Helpers;

public sealed class SqsEventSender : Rig.TUnit.Messaging.Helpers.EventSenderBase
{
    // Existing overload kept.
    public System.Threading.Tasks.Task SendAsync(
        string body,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);

    /// <summary>
    /// T030 GREEN: Maps SessionKey -> MessageGroupId, DeduplicationKey -> MessageDeduplicationId.
    /// Throws InvalidOperationException if queue URL ends ".fifo" and SessionKey is null.
    /// </summary>
    public System.Threading.Tasks.Task SendAsync(
        string body,
        Rig.TUnit.Messaging.Helpers.SendContext context,
        string? correlationId = null,
        string? causationId = null,
        string? traceparent = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? additionalHeaders = null,
        System.Threading.CancellationToken ct = default);
}

namespace Rig.TUnit.Messaging.Sqs.Builder;

public sealed class SqsRigBuilder : Rig.TUnit.Messaging.Builder.MessagingRigBuilder<SqsRigBuilder>
{
    // New in T031 GREEN.
    public SqsRigBuilder WithTopology(
        System.Action<Rig.TUnit.Messaging.Sqs.Topology.ISqsTopologyBuilder> configure);
}
