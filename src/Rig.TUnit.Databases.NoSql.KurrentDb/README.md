# Rig.TUnit.Databases.NoSql.KurrentDb

> Testcontainers-backed KurrentDB (post-rebrand EventStoreDB) fixture with `StreamAssert` for append-count verification.

## What this package is

The Rig.TUnit KurrentDB provider (EventStoreDB rebranded — see
https://www.kurrent.io/blog/kurrent-re-brand-faq). `KurrentDbFixture`
spins KurrentDB via Testcontainers and returns a `KurrentDB.Client`
ready for append / read. Ships `StreamAssert.EventsAppendedAsync` — reads
a stream forwards from start and returns the total event count
(missing streams return 0), the most common event-sourcing assertion
shape.

## When to use it

- Integration tests for event-sourced aggregates.
- Asserting event append ordering and stream revisions.
- Verifying projections against a real event log.
- **Not for**: unit tests of aggregate logic — mock
  `IEventSourcingRepository` in those.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (KurrentDB image ~400 MB)
- `KurrentDB.Client` 1.3+ (rebranded from
  `EventStore.Client.Grpc.Streams`).

## Quick start

```csharp
using Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;
using Rig.TUnit.Databases.NoSql.KurrentDb.Helpers;
using KurrentDB.Client;

await using var fx = new KurrentDbFixture();
await fx.InitializeAsync();

var stream = "order-42";
await fx.Client.AppendToStreamAsync(
    stream, StreamState.NoStream,
    new[] { new EventData(Uuid.NewUuid(), "placed", payload: "{}"u8.ToArray()) });
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"docker.kurrent.io/kurrent-latest/kurrentdb:latest"` | Image |
| `StartupTimeoutSeconds` | `int` | `180` | KurrentDB warm-up |
| `TlsSkipVerify` | `bool` | `true` | Dev-mode self-signed cert |
| `RunProjections` | `bool` | `true` | Enable category / by-event projections |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures.KurrentDbFixture`
- `Rig.TUnit.Databases.NoSql.KurrentDb.Options.KurrentDbFixtureOptions`
- `Rig.TUnit.Databases.NoSql.KurrentDb.Builder.KurrentDbRigBuilder`
- `Rig.TUnit.Databases.NoSql.KurrentDb.Assertions.StreamAssert`

## Per-test isolation

Streams are named `{IsolationKey}-{logical-stream-id}` so tests cannot
collide. No explicit teardown — KurrentDB's persistent log is discarded
when the container dies.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~15 s.
- Per-append: ~2–3 ms.
- Parallelism: 8+ concurrent tests — stream-level isolation is perfect
  for it.

## Troubleshooting

- **`StreamNotFound`** — the stream name includes `{IsolationKey}`; check
  your helper. Missing streams are valid and `StreamAssert.EventsAppended
  Async` returns 0, not throws.
- **`DEADLINE_EXCEEDED` on append** — KurrentDB under heavy parallel load
  may need raised gRPC deadline; configure via `KurrentDBClientSettings`.

See [docs/troubleshooting.md#kurrentdb](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Stream names are case-sensitive — `Order-42` and `order-42` are
  distinct.
- `StreamState.NoStream` is a different optimistic-concurrency token from
  `StreamState.Any`; tests asserting on first-append must use the former.
- Post-rebrand: package + client names changed in v1.x but wire protocol
  is backward-compatible with EventStoreDB 22+.

## Benchmarks

See [`KurrentDbBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/KurrentDbBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-008 — KurrentDb rename](../../docs/adr/ADR-008-kurrentdb-rename.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
