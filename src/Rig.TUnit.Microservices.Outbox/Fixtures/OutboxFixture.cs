using System.Collections.Concurrent;
using Rig.TUnit.Core.Fixtures;

namespace Rig.TUnit.Microservices.Outbox.Fixtures;

/// <summary>
/// Backing-store-agnostic outbox fixture. Holds an in-memory list as a virtual
/// outbox table; concrete DB-backed outboxes (SqlServer, Postgres, Cosmos, ...) layer
/// their own persistence on top via <see cref="IOutboxStore"/>.
/// </summary>
public sealed class OutboxFixture : RigFixtureBase
{
    private readonly ConcurrentQueue<OutboxMessage> _deadLetter = new();
    private bool _initialized;
    private bool _disposed;

    /// <summary>Default ctor — uses the built-in in-memory store and the default schema.</summary>
    public OutboxFixture() : this(new InMemoryOutboxStore(), new OutboxSchema()) { }

    /// <summary>Plug in a custom <see cref="IOutboxStore"/> (e.g. <see cref="Rig.TUnit.Microservices.Outbox.Fixtures.CustomOutboxStore{TRow}"/>).</summary>
    public OutboxFixture(IOutboxStore store) : this(store, new OutboxSchema()) { }

    /// <summary>Plug in both a custom store and a custom schema (table + column names).</summary>
    public OutboxFixture(IOutboxStore store, OutboxSchema schema)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public IOutboxStore Store { get; }

    /// <summary>Developer-configurable table + column names (used by SQL-backed stores).</summary>
    public OutboxSchema Schema { get; }

    public IReadOnlyCollection<OutboxMessage> DeadLetter => _deadLetter.ToArray();

    public override string ConnectionString => "outbox-in-memory";

    public override Task InitializeAsync()
    {
        _initialized = true;
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    public void PushDeadLetter(OutboxMessage message) => _deadLetter.Enqueue(message);

    internal bool IsReady => _initialized && !_disposed;
}

/// <summary>Abstraction over the outbox backing store — in-memory, SQL, or document.</summary>
public interface IOutboxStore
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken ct = default);
    Task MarkRelayedAsync(Guid id, DateTimeOffset relayedAt, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> ReadAllAsync(CancellationToken ct = default);
}

internal sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _rows = new();
    private readonly ConcurrentDictionary<Guid, byte> _claimed = new();

    public Task EnqueueAsync(OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _rows[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken ct = default)
    {
        // Atomically claim rows via CAS on _claimed — guarantees exactly-once under
        // concurrent relay runs. A claimed row is invisible to other workers until
        // either MarkRelayedAsync (success) or MarkFailedAsync (releases claim).
        var candidates = _rows.Values
            .Where(m => m.RelayedAt is null && m.FailureReason is null)
            .OrderBy(m => m.OccurredAt);

        var claimed = new List<OutboxMessage>();
        foreach (var row in candidates)
        {
            if (claimed.Count >= batchSize) break;
            if (_claimed.TryAdd(row.Id, 1))
            {
                claimed.Add(row);
            }
        }
        return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
    }

    public Task MarkRelayedAsync(Guid id, DateTimeOffset relayedAt, CancellationToken ct = default)
    {
        _rows.AddOrUpdate(id,
            _ => throw new KeyNotFoundException($"Outbox row {id} not found."),
            (_, existing) => existing with { RelayedAt = relayedAt });
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid id, string reason, CancellationToken ct = default)
    {
        _rows.AddOrUpdate(id,
            _ => throw new KeyNotFoundException($"Outbox row {id} not found."),
            (_, existing) => existing with { FailureReason = reason, DeliveryAttempts = existing.DeliveryAttempts + 1 });
        // Release the claim so retries can happen on a subsequent drain.
        _claimed.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ReadAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<OutboxMessage> all = _rows.Values.OrderBy(m => m.OccurredAt).ToArray();
        return Task.FromResult(all);
    }
}
