namespace Rig.TUnit.Messaging.Helpers;

/// <summary>
/// Optional per-message routing hints carried alongside the <c>SendContext</c> overload of
/// <see cref="EventSenderBase"/>. Each provider maps the populated fields to its native
/// ordering primitive.
/// </summary>
/// <param name="SessionKey">
/// Per-session ordering key. Maps to ServiceBus <c>SessionId</c> and SQS <c>MessageGroupId</c>.
/// On Kafka it folds to <see cref="PartitionKey"/> when that field is <see langword="null"/>.
/// </param>
/// <param name="PartitionKey">
/// Per-partition ordering key. Maps to Kafka <c>Message.Key</c>, ServiceBus
/// <c>PartitionKey</c>, and RabbitMQ routing key. Must equal <see cref="SessionKey"/> on
/// session-enabled ServiceBus entities.
/// </param>
/// <param name="DeduplicationKey">
/// Broker-level duplicate-detection key. Maps to SQS <c>MessageDeduplicationId</c>,
/// NATS JetStream <c>Nats-Msg-Id</c>, and ServiceBus <c>MessageId</c> (when
/// duplicate-detection is enabled on the entity).
/// </param>
public readonly record struct SendContext(
    string? SessionKey = null,
    string? PartitionKey = null,
    string? DeduplicationKey = null);
