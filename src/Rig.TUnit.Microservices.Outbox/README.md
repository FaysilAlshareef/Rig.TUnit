# Rig.TUnit.Microservices.Outbox

> Transactional-outbox testing helpers: `OutboxFixture`, `OutboxRelaySimulator`, `OutboxAssert`, `OutboxReplay`, + `CustomOutboxStore<TRow>` for plug-your-own-schema.

## What this package is

The command-side outbox-pattern testing kit. `OutboxFixture` owns an
`IOutboxStore` — either the default in-memory store (CAS-claim on read
for exactly-once under concurrency) or your own row type wrapped in
`CustomOutboxStore<TRow>`. `OutboxRelaySimulator` drains the pending
messages through your publish delegate; `OutboxAssert` verifies a
specific event type was enqueued and relayed; `OutboxReplay` simulates
a crashed-relay scenario to test idempotency.

Exactly-once semantics validated under 100 concurrent relay runs against
the in-memory store.

## When to use it

- Testing services that enqueue events inside the same transaction as
  the domain write.
- Verifying relay idempotency after crash-recovery.
- Asserting back-pressure / failure-handling under publish errors.
- **Not for**: full end-to-end broker tests — layer with the matching
  `Rig.TUnit.Messaging.*` provider.

## Prerequisites

- .NET 10 SDK
- If using `CustomOutboxStore<TRow>`: your persistence layer (EF Core,
  Dapper, Marten, …).

## Quick start

```csharp
using Rig.TUnit.Microservices.Outbox;

await using var fx = new OutboxFixture();
await fx.InitializeAsync();
await fx.Store.EnqueueAsync(new OutboxMessage(
    Guid.NewGuid(), "agg-1", "OrderPlaced", "{}", DateTimeOffset.UtcNow));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `TableName` | `string` | `"Outbox"` | Schema table name for SQL stores |
| `BatchSize` | `int` | `100` | Rows per `ReadPendingAsync` claim |
| `ClaimTtl` | `TimeSpan` | `5m` | How long a claimed row stays hidden |
| `PublishRetries` | `int` | `3` | Retries on publish failure before dead-letter |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.Outbox.Fixtures.OutboxFixture`
- `Rig.TUnit.Microservices.Outbox.Stores.IOutboxStore`
- `Rig.TUnit.Microservices.Outbox.Stores.CustomOutboxStore<TRow>`
- `Rig.TUnit.Microservices.Outbox.Helpers.OutboxRelaySimulator`
- `Rig.TUnit.Microservices.Outbox.Helpers.OutboxReplay`
- `Rig.TUnit.Microservices.Outbox.Assertions.OutboxAssert`
- `Rig.TUnit.Microservices.Outbox.Schema.OutboxSchema`

## Per-test isolation

`OutboxFixture` owns its store — in-memory is scoped per-fixture, and
`CustomOutboxStore<TRow>` inherits the isolation of the persistence
layer you plug in. `IsolationKey` can be used as the outbox table suffix
for parallel SQL-backed runs.

## Parallelism + performance

- In-memory store: `EnqueueAsync` ~200 ns, `ReadPendingAsync` ~1 µs.
- `OutboxRelaySimulator.DrainAsync` is sequential by design — exactly-
  once guarantees require CAS-claim + single-writer-per-batch.
- Parallelism across fixtures: safe; each owns its store.

## Troubleshooting

- **Duplicate publish** — claim TTL expired before publish completed; a
  second relay picked up the row. Raise `ClaimTtl` or speed up the
  publish delegate.
- **`OutboxAssert.Contains<T>` reports not-found** — the message's
  `Type` string doesn't match `typeof(T).FullName`; either rename or
  override `GetTypeName(Type)`.

See [docs/troubleshooting.md#outbox](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `OutboxSchema` ships pre-built parameterised SQL for the common cases
  (`BuildInsertSql`, `BuildReadPendingSql(N)`, etc). Non-standard
  providers (Cosmos, Mongo) need custom implementations of
  `IOutboxStore`.
- Relay simulator drains to exhaustion by default — large backlogs can
  slow tests. Use `relay.DrainAsync(maxBatches: 10)` for bounded runs.

## Benchmarks

See [`OutboxBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/OutboxBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. Exactly-once-under-100-
concurrent-relays is the marquee test.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Microservices.Inbox`](../Rig.TUnit.Microservices.Inbox/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
