namespace Rig.TUnit.Microservices.Outbox;

/// <summary>Persisted outbox row. Relays drain rows in order into the messaging provider.</summary>
public sealed record OutboxMessage(
    Guid Id,
    string AggregateId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt,
    string? CorrelationId = null,
    string? CausationId = null,
    string? Traceparent = null,
    DateTimeOffset? RelayedAt = null,
    string? FailureReason = null,
    int DeliveryAttempts = 0);

/// <summary>Envelope published onto the bus — serialised form of an <see cref="OutboxMessage"/>.</summary>
public sealed record OutboxEventEnvelope(
    Guid MessageId,
    string AggregateId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    string? CausationId,
    string? Traceparent);
