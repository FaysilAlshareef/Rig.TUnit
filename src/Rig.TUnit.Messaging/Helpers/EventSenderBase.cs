namespace Rig.TUnit.Messaging.Helpers;

/// <summary>
/// Generic event-sender skeleton that concrete providers extend. Handles W3C
/// <c>traceparent</c> generation and correlation/causation header propagation.
/// </summary>
public abstract class EventSenderBase
{
    protected static string NewTraceparent()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var spanId = Guid.NewGuid().ToString("N")[..16];
        return $"00-{traceId}-{spanId}-01";
    }

    /// <summary>
    /// Legacy overload. Builds the correlation/causation/traceparent header envelope and
    /// merges any <paramref name="additional"/> entries on top.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, string> BuildHeaders(
        string? correlationId,
        string? causationId,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additional = null)
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        if (correlationId is not null) headers["x-correlation-id"] = correlationId;
        if (causationId is not null) headers["x-causation-id"] = causationId;
        headers["traceparent"] = traceparent ?? NewTraceparent();
        if (additional is not null)
        {
            foreach (var kv in additional)
            {
                headers[kv.Key] = kv.Value;
            }
        }
        return headers;
    }

    /// <summary>
    /// SendContext-aware overload. Produces the same header envelope as the legacy overload
    /// when <paramref name="context"/> is <c>default</c>; concrete providers map the
    /// context fields to SDK-native routing hints outside the header dictionary.
    /// </summary>
    /// <param name="context">Per-message routing hints (session / partition / deduplication keys).</param>
    /// <param name="correlationId">Optional correlation identifier, written to <c>x-correlation-id</c>.</param>
    /// <param name="causationId">Optional causation identifier, written to <c>x-causation-id</c>.</param>
    /// <param name="traceparent">Optional W3C traceparent. Generated fresh when <see langword="null"/>.</param>
    /// <param name="additional">Optional extra headers merged on top of the standard envelope.</param>
    protected virtual IReadOnlyDictionary<string, string> BuildHeaders(
        SendContext context,
        string? correlationId,
        string? causationId,
        string? traceparent = null,
        IReadOnlyDictionary<string, string>? additional = null)
    {
        _ = context;
        return BuildHeaders(correlationId, causationId, traceparent, additional);
    }
}
