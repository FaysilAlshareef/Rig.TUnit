# Rig.TUnit.Microservices.Inbox

> Inbox pattern — per-aggregate sequence tracking for idempotent event application. `SequenceTracker` + fluent `InboxAssert.SequenceApplied(…).Idempotent()`.

## What this package is

The query-side idempotency helper for event-sourced microservices.
`SequenceTracker` keeps a per-aggregate "highest applied sequence"
record; `TryApply(aggregateId, sequence)` returns `true` only for
strictly-increasing sequences. Duplicates and out-of-order events are
rejected. `InboxAssert` offers the fluent assertion shape tests expect.

## When to use it

- Testing event-handler idempotency.
- Verifying out-of-order events are rejected.
- Regression-guarding "at-least-once delivery" correctness after a
  consumer rewrite.
- **Not for**: integration testing of a real DB-backed inbox — extend
  `SequenceTracker` with your persistence adapter.

## Prerequisites

- .NET 10 SDK

## Quick start

```csharp
using Rig.TUnit.Microservices.Inbox;

var tracker = new SequenceTracker();
tracker.TryApply("agg-1", 5);          // true
tracker.TryApply("agg-1", 5);          // false — idempotent re-apply
tracker.TryApply("agg-1", 4);          // false — out of order

InboxAssert.SequenceApplied(tracker, "agg-1", 5).Idempotent();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `MaxTrackedAggregates` | `int` | `10_000` | Guard against unbounded growth |
| `EvictionPolicy` | `InboxEvictionPolicy` | `OldestFirst` | How to reclaim slots when full |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.Inbox.SequenceTracker`
- `Rig.TUnit.Microservices.Inbox.Assertions.InboxAssert`

## Per-test isolation

Each `SequenceTracker` instance is scoped per-test. No shared statics.
Safe under full parallelism.

## Parallelism + performance

- `TryApply` is a dictionary lookup + CAS: ~50 ns per call.
- Thread-safe via `ConcurrentDictionary`; safe under full parallelism.

## Troubleshooting

- **`TryApply` returns `true` for a duplicate** — the aggregate-id
  string is different (case, whitespace). `SequenceTracker` is
  case-sensitive by design; normalise at your persistence boundary.

See [docs/troubleshooting.md#inbox](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Sequence numbers are `long`; negative sequences throw
  `ArgumentOutOfRangeException`.
- Gaps are not detected — `TryApply(5)` after `TryApply(3)` returns
  `true`, and the tracker advances. That is the correct semantic for
  most inbox patterns (gaps are the publisher's problem).

## Benchmarks

See [`InboxBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/InboxBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Microservices.Outbox`](../Rig.TUnit.Microservices.Outbox/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
