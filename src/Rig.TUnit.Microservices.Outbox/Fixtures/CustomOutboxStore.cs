namespace Rig.TUnit.Microservices.Outbox.Fixtures;

/// <summary>
/// Adapter that lets developers reuse their existing outbox row type without
/// re-modelling. Provide a pair of mappers (to/from <see cref="OutboxMessage"/>)
/// plus the async operations against your store. The relay simulator and
/// assertions work unchanged.
///
/// <example>
/// <code>
/// var store = CustomOutboxStore&lt;MyOutboxRow&gt;.Create(
///     mapToMessage:   row =&gt; new OutboxMessage(row.Id, row.AggId, row.Type, row.Json, row.Timestamp),
///     mapFromMessage: m   =&gt; new MyOutboxRow { Id = m.Id, AggId = m.AggregateId /* ... */ },
///     enqueueAsync:   (row, ct) =&gt; _db.OutboxRows.AddAsync(row, ct).AsTask(),
///     readPendingAsync: (take, ct) =&gt; _db.OutboxRows
///                                          .Where(r =&gt; r.RelayedAt == null)
///                                          .OrderBy(r =&gt; r.OccurredAt)
///                                          .Take(take)
///                                          .ToListAsync(ct),
///     markRelayedAsync: (id, at, ct) =&gt; _db.OutboxRows
///                                          .Where(r =&gt; r.Id == id)
///                                          .ExecuteUpdateAsync(u =&gt; u.SetProperty(x =&gt; x.RelayedAt, at), ct),
///     markFailedAsync:  (id, reason, ct) =&gt; _db.OutboxRows
///                                          .Where(r =&gt; r.Id == id)
///                                          .ExecuteUpdateAsync(u =&gt; u.SetProperty(x =&gt; x.Reason, reason), ct));
/// </code>
/// </example>
/// </summary>
public sealed class CustomOutboxStore<TRow> : IOutboxStore
{
    /// <summary>
    /// Fallback page size used for <see cref="ReadAllAsync"/> when the developer
    /// does not supply a dedicated read-all delegate. Pagination is enforced —
    /// callers must page through results themselves if the store holds more rows.
    /// </summary>
    public const int ReadAllPageSize = 10_000;

    private readonly Func<TRow, OutboxMessage> _mapToMessage;
    private readonly Func<OutboxMessage, TRow> _mapFromMessage;
    private readonly Func<TRow, CancellationToken, Task> _enqueueAsync;
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<TRow>>> _readPendingAsync;
    private readonly Func<Guid, DateTimeOffset, CancellationToken, Task> _markRelayedAsync;
    private readonly Func<Guid, string, CancellationToken, Task> _markFailedAsync;
    private readonly Func<int, CancellationToken, Task<IReadOnlyList<TRow>>>? _readAllPageAsync;

    private CustomOutboxStore(
        Func<TRow, OutboxMessage> mapToMessage,
        Func<OutboxMessage, TRow> mapFromMessage,
        Func<TRow, CancellationToken, Task> enqueueAsync,
        Func<int, CancellationToken, Task<IReadOnlyList<TRow>>> readPendingAsync,
        Func<Guid, DateTimeOffset, CancellationToken, Task> markRelayedAsync,
        Func<Guid, string, CancellationToken, Task> markFailedAsync,
        Func<int, CancellationToken, Task<IReadOnlyList<TRow>>>? readAllPageAsync)
    {
        _mapToMessage = mapToMessage;
        _mapFromMessage = mapFromMessage;
        _enqueueAsync = enqueueAsync;
        _readPendingAsync = readPendingAsync;
        _markRelayedAsync = markRelayedAsync;
        _markFailedAsync = markFailedAsync;
        _readAllPageAsync = readAllPageAsync;
    }

    /// <param name="readAllPageAsync">
    /// Optional delegate that reads a bounded page of rows (including relayed/failed)
    /// for inspection. Must apply <c>Take(pageSize)</c> — never return the entire table.
    /// </param>
    public static CustomOutboxStore<TRow> Create(
        Func<TRow, OutboxMessage> mapToMessage,
        Func<OutboxMessage, TRow> mapFromMessage,
        Func<TRow, CancellationToken, Task> enqueueAsync,
        Func<int, CancellationToken, Task<IReadOnlyList<TRow>>> readPendingAsync,
        Func<Guid, DateTimeOffset, CancellationToken, Task> markRelayedAsync,
        Func<Guid, string, CancellationToken, Task> markFailedAsync,
        Func<int, CancellationToken, Task<IReadOnlyList<TRow>>>? readAllPageAsync = null)
    {
        ArgumentNullException.ThrowIfNull(mapToMessage);
        ArgumentNullException.ThrowIfNull(mapFromMessage);
        ArgumentNullException.ThrowIfNull(enqueueAsync);
        ArgumentNullException.ThrowIfNull(readPendingAsync);
        ArgumentNullException.ThrowIfNull(markRelayedAsync);
        ArgumentNullException.ThrowIfNull(markFailedAsync);
        return new CustomOutboxStore<TRow>(
            mapToMessage, mapFromMessage, enqueueAsync, readPendingAsync,
            markRelayedAsync, markFailedAsync, readAllPageAsync);
    }

    public Task EnqueueAsync(OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _enqueueAsync(_mapFromMessage(message), ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken ct = default)
    {
        var rows = await _readPendingAsync(batchSize, ct);
        return rows.Select(_mapToMessage).ToArray();
    }

    public Task MarkRelayedAsync(Guid id, DateTimeOffset relayedAt, CancellationToken ct = default)
        => _markRelayedAsync(id, relayedAt, ct);

    public Task MarkFailedAsync(Guid id, string reason, CancellationToken ct = default)
        => _markFailedAsync(id, reason, ct);

    /// <summary>
    /// Bounded read-all: returns up to <see cref="ReadAllPageSize"/> rows. Callers
    /// needing more pages should supply a dedicated <c>readAllPageAsync</c> delegate
    /// and call <see cref="ReadPageAsync"/>.
    /// </summary>
    public Task<IReadOnlyList<OutboxMessage>> ReadAllAsync(CancellationToken ct = default)
        => ReadPageAsync(ReadAllPageSize, ct);

    public async Task<IReadOnlyList<OutboxMessage>> ReadPageAsync(int pageSize, CancellationToken ct = default)
    {
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var rows = _readAllPageAsync is not null
            ? await _readAllPageAsync(pageSize, ct)
            : await _readPendingAsync(pageSize, ct);
        return rows.Select(_mapToMessage).ToArray();
    }
}
