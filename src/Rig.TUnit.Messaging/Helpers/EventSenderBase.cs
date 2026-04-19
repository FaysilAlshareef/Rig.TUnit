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
}
