using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Assertions;

/// <summary>
/// Assertion helpers for verifying per-key message ordering across all messaging providers.
/// </summary>
/// <remarks>
/// <para>
/// Provider capability matrix — which <see cref="Helpers.SendContext"/> field drives ordering
/// and which listener property to use as <c>keyExtractor</c> in <see cref="PerKeyMonotonic{T}"/>:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Provider</term>
///     <description>Ordering primitive · SendContext field · keyExtractor · sequenceExtractor</description>
///   </listheader>
///   <item>
///     <term>Azure Service Bus</term>
///     <description>
///       Session FIFO via native <c>SessionProcessor</c>.<br/>
///       Field: <c>SessionKey</c>.<br/>
///       <c>keyExtractor</c>: <c>m => m.SessionId</c><br/>
///       <c>sequenceExtractor</c>: <c>m => m.SequenceNumber</c>
///     </description>
///   </item>
///   <item>
///     <term>Apache Kafka</term>
///     <description>
///       Deterministic partition via Confluent.Kafka's default murmur2 partitioner.<br/>
///       Field: <c>PartitionKey</c> (falls back to <c>SessionKey</c> when null).<br/>
///       <c>keyExtractor</c>: <c>m => m.Message.Key</c><br/>
///       <c>sequenceExtractor</c>: <c>m => m.Offset.Value</c>
///     </description>
///   </item>
///   <item>
///     <term>Amazon SQS</term>
///     <description>
///       FIFO queue <c>MessageGroupId</c>.<br/>
///       Field: <c>SessionKey</c> → <c>MessageGroupId</c>.<br/>
///       <c>keyExtractor</c>: <c>m => m.Attributes["MessageGroupId"]</c><br/>
///       <c>sequenceExtractor</c>: <c>m => long.Parse(m.Attributes["SequenceNumber"])</c>
///     </description>
///   </item>
///   <item>
///     <term>RabbitMQ</term>
///     <description>
///       Topic-exchange routing key; single-queue FIFO within a binding.<br/>
///       Field: <c>PartitionKey</c> → AMQP routing key.<br/>
///       <c>keyExtractor</c>: <c>m => m.RoutingKey</c><br/>
///       <c>sequenceExtractor</c>: <c>m => m.DeliveryTag</c> (monotonic per channel)
///     </description>
///   </item>
///   <item>
///     <term>NATS JetStream</term>
///     <description>
///       Ordered consumer (deliver-all, replay-instant, explicit ack).<br/>
///       Field: <c>SessionKey</c> → <c>x-session-key</c> NATS header; surfaced in
///       <see cref="Helpers.CapturedMessage{T}.SessionKey"/>.<br/>
///       <c>keyExtractor</c>: <c>m => m.Headers?["x-session-key"] ?? string.Empty</c><br/>
///       <c>sequenceExtractor</c>: use <c>CapturedMessage.ReceivedAt.Ticks</c> or
///       the JetStream sequence from <c>NatsJSMsg.Metadata.Sequence.Stream</c>.
///     </description>
///   </item>
/// </list>
/// <para>See <c>docs/ordering-assertions.md</c> for the full matrix, usage examples, and provider notes.</para>
/// </remarks>
public sealed class OrderingAssert
{
    private OrderingAssert() { }

    /// <summary>
    /// Asserts that messages captured by <paramref name="listener"/> with the same key (as
    /// returned by <paramref name="keyExtractor"/>) arrived in monotonically non-decreasing
    /// order according to <paramref name="sequenceExtractor"/>.
    /// </summary>
    /// <typeparam name="T">The provider-specific message record type.</typeparam>
    /// <param name="listener">The listener whose captured messages are inspected.</param>
    /// <param name="keyExtractor">
    /// Extracts the per-key group identifier from a message. Use the provider-native ordering
    /// primitive (session ID, partition key, message-group ID, routing key, or session-key header).
    /// See the <see cref="OrderingAssert"/> class remarks for a per-provider reference.
    /// </param>
    /// <param name="sequenceExtractor">
    /// Extracts a monotonically increasing sequence number from a message (broker sequence,
    /// offset, delivery tag, or timestamp ticks).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any message within a key group arrives with a sequence number lower than
    /// the previous message in the same group.
    /// </exception>
    public static void PerKeyMonotonic<T>(
        ListenerBase<T> listener,
        Func<T, string> keyExtractor,
        Func<T, long> sequenceExtractor) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(keyExtractor);
        ArgumentNullException.ThrowIfNull(sequenceExtractor);

        var byKey = listener.Captured
            .GroupBy(m => keyExtractor(m.Message), StringComparer.Ordinal)
            .ToArray();

        foreach (var group in byKey)
        {
            long last = long.MinValue;
            foreach (var m in group)
            {
                var seq = sequenceExtractor(m.Message);
                if (seq < last)
                {
                    throw new InvalidOperationException($"OrderingAssert.PerKeyMonotonic: key '{group.Key}' saw {seq} after {last}");
                }
                last = seq;
            }
        }
    }
}
