# Rig.TUnit.Parallelism

> `ParallelIsolationContract` — family-level suite asserting a provider fixture behaves correctly under concurrent `TUnit` parallel execution.

## What this package is

A small but load-bearing contract suite. Every provider that claims to be
parallel-safe must inherit `ParallelIsolationContract<TFixture>` and register
it via `[InheritsTests]` at the family level. The contract drives N parallel
test iterations, observes whether they trample each other's state, and
reports both the observed concurrency (did they actually run in parallel?)
and the correctness (did they isolate?).

Combined with `IsolationKey`, this is the package that keeps the "fixtures
are parallel-safe" claim honest across 30+ providers.

## When to use it

- Writing a new provider — always add a concrete subclass of
  `ParallelIsolationContract<TFixture>` to your integration project.
- Regression-testing an existing provider that historically needed
  `[NotInParallel]` — the contract is how you prove the marker can be
  removed.
- **Not for**: unit tests. The contract is integration-only (spins real
  fixtures).

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (the provider's container backend).

## Quick start

```csharp
using Rig.TUnit.Parallelism.Helpers;

var report = await ParallelIsolationHarness.RunAsync(
    iterations: 8,
    buildFixture: () => new InMemoryFixture());

await Assert.That(report.MaxConcurrency).IsGreaterThan(1);
await Assert.That(report.StateCollisions).IsEqualTo(0);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Iterations` | `int` | `8` | Number of parallel test instances. |
| `PerIterationTimeout` | `TimeSpan` | `30s` | Max time per concurrent run before harness aborts. |
| `RequireActualParallelism` | `bool` | `true` | Fail if `MaxConcurrency` is 1 — that indicates a global lock is serialising work. |

## Fixture + helper APIs

- `Rig.TUnit.Parallelism.Helpers.ParallelIsolationHarness` — test driver
- `Rig.TUnit.Parallelism.Helpers.ParallelIsolationReport` — results shape

## Per-test isolation

Each iteration runs in its own `Task`; the harness captures collision
evidence (shared mutable state observed) and reports per-iteration timing so
a regression (e.g., accidental global lock) is visible immediately.

## Parallelism + performance

- Harness overhead: ~10 ms setup + per-iteration fixture cost.
- The contract runs every provider's full fixture under `Iterations=8` by
  default; adjust down for expensive providers (Oracle, Cosmos) via config.

## Troubleshooting

- **`MaxConcurrency=1`** — something serialised the runs. Common causes:
  `[NotInParallel]` marker, a `static readonly SemaphoreSlim` shared across
  fixtures, or a global Docker network with a single bind-port.
- **`StateCollisions > 0`** — two iterations observed the same container or
  schema name. Re-check `IsolationKey` threading in the provider's fixture.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Kafka's broker port binds a single host port per container — parallelism is
  bounded by the number of distinct bind-ports you allocate (see
  `Rig.TUnit.Messaging.Kafka`'s README).
- Cosmos emulator is Linux-only in its parallel mode; Windows runs
  serialised (`Iterations = 1`).

## Benchmarks

See [`ParallelismBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ParallelismBenchmarks.cs);
tracked in `benchmarks/baseline-005.json`.

## Related docs

- [ADR-006 — IsolationKey](../../docs/adr/ADR-006-isolationkey.md)
- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
