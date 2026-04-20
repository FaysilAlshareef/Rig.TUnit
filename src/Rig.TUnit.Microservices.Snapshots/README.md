# Rig.TUnit.Microservices.Snapshots

> Snapshot-testing assertion compatible with Verify.TUnit file naming, with microservice-opinionated scrubbers.

## What this package is

A lightweight snapshot-testing harness compatible with Verify.TUnit
file-naming convention (`{name}.received.*` / `{name}.verified.*`).
What it adds on top is an opinionated, microservice-shaped scrubber
pipeline: correlation/causation IDs, event IDs, timestamps, sequence
numbers, connection strings, and filesystem paths become deterministic
placeholders before comparison. Without this, every snapshot test would
drift on every run.

## When to use it

- Asserting the exact JSON shape of a REST / gRPC response.
- Verifying event-envelope layouts match the documented contract.
- Regression-guarding refactors that should preserve output byte-for-byte.
- **Not for**: live file-system state — `SnapshotAssert` is for
  structured value comparison.

## Prerequisites

- .NET 10 SDK
- `Verify.TUnit` (transitive)

## Quick start

```csharp
using Rig.TUnit.Microservices.Snapshots;

var payload = new { Id = Guid.NewGuid(), At = DateTimeOffset.UtcNow, Total = 42 };
var result = await SnapshotAssert.MatchJson(
    payload, name: "order-created", directory: "__snapshots__");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `AutoVerify` | `bool` | `false` | First-run auto-create `{name}.verified.*` (off by default — review) |
| `ScrubExtraPatterns` | `string[]?` | `null` | Extra regex patterns to scrub beyond defaults |
| `DiffOnFailure` | `bool` | `true` | Include line-diff in exception message |

## Fixture + helper APIs

- `Rig.TUnit.Microservices.Snapshots.SnapshotAssert`
- `Rig.TUnit.Microservices.Snapshots.Scrubbers.DefaultScrubbers`
- `Rig.TUnit.Microservices.Snapshots.Options.SnapshotOptions`

## Per-test isolation

Snapshot files are named by the `name` argument + the test file's
directory — tests cannot collide by design. Verify.TUnit's naming
convention is preserved.

## Parallelism + performance

- Per-assertion: ~1 ms for a ~1 KB payload (serialise + scrub + file
  compare).
- Safe under full parallelism — file I/O is per-test scoped.

## Troubleshooting

- **Snapshot drifts every run** — a scrubber missed a volatile field.
  Add it via `ScrubExtraPatterns` or file a bug if it looks like a
  common shape.
- **`SnapshotAssertionException`** — inspect the `.received.*` file
  side-by-side with `.verified.*`; the exception message includes a
  line-diff by default.

See [docs/troubleshooting.md#snapshots](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Default scrubbers: GUID, ISO-8601 timestamp, `CorrelationId` /
  `CausationId` values, `EventId` / `MessageId`, `Sequence` numeric,
  SQL Server connection strings, Windows/Unix absolute paths. Each is
  replaced with a deterministic placeholder.
- Scrubbers are applied in order; later scrubbers see placeholders
  written by earlier ones.
- JSON property order is preserved — snapshot mismatches triggered by
  reordering are considered intentional.

## Benchmarks

See [`SnapshotsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SnapshotsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Sibling: [`Rig.TUnit.Microservices.EventSourcing`](../Rig.TUnit.Microservices.EventSourcing/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
