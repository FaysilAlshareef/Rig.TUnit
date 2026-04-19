namespace Rig.TUnit.Messaging;

/// <summary>
/// Transport-agnostic envelope carrying a message payload plus correlation/causation
/// identifiers and W3C <c>traceparent</c> header for distributed-trace propagation.
/// </summary>
public sealed record EventEnvelope(
    string MessageId,
    string MessageType,
    string Body,
    string? CorrelationId,
    string? CausationId,
    string? Traceparent,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset Timestamp);
