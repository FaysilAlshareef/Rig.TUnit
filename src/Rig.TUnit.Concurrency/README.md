# Rig.TUnit.Concurrency

> Concurrency + idempotency helpers: `ConcurrencyAssert.TwoWriters`, `Precondition.IfMatchFails`/`NotModified`, `SequenceIdempotencyChecker`.

## What this package is

A small toolbox for the three most common concurrency-test shapes:
1. Two-writers-one-wins (optimistic concurrency) — parameterised on the
   exception type so the same assertion works against EF Core's
   `DbUpdateConcurrencyException`, Mongo's `MongoWriteException`, Cosmos's
   `412 Precondition Failed`, etc.
2. HTTP ETag / If-Match — verify 412 on stale precondition and 304 on
   fresh one.
3. Sequence idempotency — verify replaying the same sequence produces
   identical outcomes.

No containers, no dependencies beyond Rig.TUnit.Core — pure harness code.

## When to use it

- Testing optimistic-concurrency writes across heterogeneous stores.
- Verifying HTTP cache-control semantics end-to-end.
- Regression-guarding event-replay idempotency.
- **Not for**: unit-testing lock contention — use
  `Rig.TUnit.Parallelism.ParallelIsolationContract` for that.

## Prerequisites

- .NET 10 SDK.

## Quick start

```csharp
using Rig.TUnit.Concurrency;
using Microsoft.EntityFrameworkCore;

await ConcurrencyAssert.TwoWriters(order)
    .OneWinsWith<DbUpdateConcurrencyException>(
        a => a.TryUpdateAsync(newValue: 1),
        b => b.TryUpdateAsync(newValue: 2));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `TimeoutPerBranch` | `TimeSpan` | `10s` | Per-writer deadline in `TwoWriters` |
| `ExpectedLosers` | `int` | `1` | How many branches should raise the concurrency exception |
| `PollingInterval` | `TimeSpan` | `50ms` | Back-off for async sequence checks |

## Fixture + helper APIs

- `Rig.TUnit.Concurrency.ConcurrencyAssert`
- `Rig.TUnit.Concurrency.Precondition`
- `Rig.TUnit.Concurrency.SequenceIdempotencyChecker`

## Per-test isolation

All helpers are stateless — every call constructs its own harness.
Safe under full parallelism.

## Parallelism + performance

- `TwoWriters` dispatches two `Task`s and observes which throws.
- Overhead: ~0.5 ms per assertion plus the cost of the writes.
- Safe under full parallelism.

## Troubleshooting

- **Both writers succeed** — your store does not enforce optimistic
  concurrency at the row/document level. Fix the schema (add
  concurrency token / ETag) before re-running.
- **`TimeoutPerBranch` exceeded** — one writer blocked indefinitely;
  likely a missing cancellation propagation in the code under test.

See [docs/troubleshooting.md#concurrency](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `OneWinsWith<TException>` catches the first matching exception type
  from either branch; if your store wraps the underlying exception,
  match the outermost type or use a base class.
- `Precondition.IfMatchFails` expects HTTP status 412; some gateways
  translate to 409 — pass `expectedStatus: HttpStatusCode.Conflict` to
  override.

## Benchmarks

See [`ConcurrencyBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ConcurrencyBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Parallelism`](../Rig.TUnit.Parallelism/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
