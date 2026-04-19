using Rig.TUnit.Microservices.Outbox.Fixtures;

namespace Rig.TUnit.Microservices.Outbox;

/// <summary>
/// Replays previously-relayed outbox rows in their original <c>OccurredAt</c> order,
/// optionally filtered by timestamp range. Used for disaster recovery rehearsal
/// and projector back-fill testing.
/// </summary>
public sealed class OutboxReplay
{
    private readonly IOutboxStore _store;
    private readonly Func<OutboxEventEnvelope, CancellationToken, Task> _publish;

    public OutboxReplay(IOutboxStore store, Func<OutboxEventEnvelope, CancellationToken, Task> publish)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
    }

    public async Task<int> ReplayAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
    {
        var all = await _store.ReadAllAsync(ct);
        var filtered = all.Where(m =>
                (from is null || m.OccurredAt >= from)
                && (to is null || m.OccurredAt <= to))
            .OrderBy(m => m.OccurredAt)
            .ToArray();

        foreach (var row in filtered)
        {
            ct.ThrowIfCancellationRequested();
            var envelope = new OutboxEventEnvelope(
                row.Id, row.AggregateId, row.EventType, row.Payload, row.OccurredAt,
                row.CorrelationId, row.CausationId, row.Traceparent);
            await _publish(envelope, ct);
        }
        return filtered.Length;
    }
}
