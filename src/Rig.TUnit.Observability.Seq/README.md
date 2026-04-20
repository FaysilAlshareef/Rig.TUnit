# Rig.TUnit.Observability.Seq

> Testcontainers-backed Seq server with Serilog sink — logs land in a real Seq instance; `SeqAssert.Query(…).Count(N).Within(…)` for fluent query-based verification.

## What this package is

Boots a `datalust/seq` Testcontainer and wires a Serilog Seq sink so
logs emitted through the fixture's `ILoggerFactory` actually land in
Seq. Tests then assert via Seq's query language:
`SeqAssert.Query(fx, "Level=@Warning").Count(1).Within(10s)`.
`CaptureDashboardSnapshotAsync` writes a `.txt` artefact to
`TestResults/seq-dashboards/` with the Seq URL and metadata for
post-failure inspection.

## When to use it

- Integration tests where Serilog-structured logs matter end-to-end.
- Regression-testing a Serilog configuration under real ingestion.
- Producing failure-artefact URLs pointing into Seq for triage.
- **Not for**: unit tests — use `Rig.TUnit.Observability.Logging` for
  in-memory capture.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Seq image ~250 MB)
- `Serilog` + `Serilog.Sinks.Seq` (transitive)

## Quick start

```csharp
using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Seq.Fixtures;
using Rig.TUnit.Observability.Seq.Options;

await using var fx = new SeqFixture(new SeqFixtureOptions { ImageTag = "latest" });
await fx.InitializeAsync();

fx.Factory.CreateLogger("T").LogWarning("Disk low {Free}", 100);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ImageTag` | `string` | `"latest"` | Seq image tag |
| `StartupTimeoutSeconds` | `int` | `60` | Seq boot window |
| `AcceptEula` | `bool` | `true` | Seq EULA acceptance |
| `MinimumLevel` | `string` | `"Information"` | Serilog filter |

## Fixture + helper APIs

- `Rig.TUnit.Observability.Seq.Fixtures.SeqFixture`
- `Rig.TUnit.Observability.Seq.Options.SeqFixtureOptions`
- `Rig.TUnit.Observability.Seq.Builder.SeqRigBuilder`
- `Rig.TUnit.Observability.Seq.Assertions.SeqAssert`

## Per-test isolation

Each fixture runs its own Seq container — no cross-test data bleed.
Logs include `ServiceName = rigtunit-{IsolationKey}` so multiple
parallel Seq instances stay distinct.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~5 s.
- Per-log overhead: ~3–5 ms (Serilog batching typically hides this).
- Parallelism: limited by container cost; 2–4 concurrent fixtures
  typical.

## Troubleshooting

- **`SeqAssert.Query(…).Count(1)` times out** — Seq is eventually
  consistent for index builds; default `Within` is 10 s, which is
  usually enough. Raise for heavy load.
- **Dashboard snapshot missing** — `CaptureDashboardSnapshotAsync`
  writes to `TestResults/seq-dashboards/{testName}.txt`; ensure the
  test runner's working directory is the project root.

See [docs/troubleshooting.md#seq](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Seq query language is its own DSL — `Level=@Warning`,
  `OrderId=42`; see Seq docs for the full grammar.
- The container's admin password is randomised per fixture; API keys
  are pre-created by the fixture so tests don't deal with auth.

## Benchmarks

See [`SeqBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SeqBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Observability`](../Rig.TUnit.Observability/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
