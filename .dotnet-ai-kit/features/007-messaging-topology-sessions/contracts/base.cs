// Contract snapshot — Phase 0 base library.
// Production counterparts:
//   src/Rig.TUnit.Messaging/Helpers/SendContext.cs
//   src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs
//   src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs
//   src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs

namespace Rig.TUnit.Messaging.Helpers;

/// <summary>
/// Optional per-message routing hints. Providers map populated fields to their native
/// ordering primitive. See <see cref="Rig.TUnit.Messaging.Assertions.OrderingAssert.PerKeyMonotonic"/>
/// for how the captured key is asserted.
/// </summary>
public readonly record struct SendContext(
    string? SessionKey = null,
    string? PartitionKey = null,
    string? DeduplicationKey = null);

public abstract class EventSenderBase
{
    // Existing overload — kept as-is.
    protected virtual IReadOnlyDictionary<string, string> BuildHeaders(
        string? correlationId,
        string? causationId,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additional = null);

    // New overload landed in T000 GREEN.
    protected virtual IReadOnlyDictionary<string, string> BuildHeaders(
        SendContext context,
        string? correlationId,
        string? causationId,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additional = null);
}

public sealed record CapturedMessage<TMessage>(
    TMessage Message,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string> Headers,
    string Body,                     // narrowed from string? (C-001)
    string? CorrelationId,
    string? SessionKey = null);      // new trailing optional

namespace Rig.TUnit.Messaging.Topology;

/// <summary>
/// Marker / application hook implemented by every provider-specific topology builder.
/// No fluent methods here — those live on the provider-specific sub-interface (C-003).
/// </summary>
public interface ITopologyBuilder
{
    Task ApplyAsync(CancellationToken ct);
}
