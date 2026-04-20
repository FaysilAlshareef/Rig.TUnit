# Rig.TUnit.Ci

> CI-aware enrichers that auto-annotate test output with build metadata (commit SHA, PR number, matrix cell).

## What this package is

A small set of enrichers that detect the ambient CI environment (GitHub
Actions, Azure DevOps, local) and enrich every `ILogger` scope, `Activity`
tag, and TRX metadata entry with `BUILD_ID`, `COMMIT_SHA`, `PR_NUMBER`, and
`MATRIX_CELL` so post-failure triage from an artefact is always possible
without pawing through raw workflow logs.

It is the "glue" that makes coverage reports, TRX files, and captured logs
cross-correlatable on a failed run.

## When to use it

- Any test project that runs in CI and produces artefacts (TRX, cobertura,
  benchmark JSON) that need to be traceable back to their PR.
- Integration projects where you want `Activity` tags to include
  `ci.commit_sha` so distributed traces survive the trip through Seq/OTLP.
- **Not for**: pure domain-unit projects that never run in CI.

## Prerequisites

- .NET 10 SDK
- A test project already using `Microsoft.Extensions.Logging` or OpenTelemetry
  `ActivitySource` instrumentation.

## Quick start

```csharp
using Rig.TUnit.Ci.Enrichers;

var scope = CiHelpers.BeginCiScope();
// Every ILogger call inside this scope now has ci.commit_sha / ci.pr_number
// properties attached via ambient enrichment.
scope.Dispose();
```

## Options

## §6 — N/A: environment-variable driven (`GITHUB_SHA`, `GITHUB_RUN_ID`,
`GITHUB_PR_NUMBER`, `MATRIX_CELL`); no `FixtureOptions` class because there
is nothing to configure beyond the ambient CI vars.

## Fixture + helper APIs

- `Rig.TUnit.Ci.Enrichers.CiHelpers` — detect + enrich entry point
- `CiHelpers.BeginCiScope()` — push an ambient scope for tests

## Per-test isolation

No per-test state; enrichers read environment variables once per process.
Safe across `[NotInParallel]` and `[NotInParallel(Order = N)]`.

## Parallelism + performance

Zero measurable overhead — first call caches the detected CI environment in a
`static readonly` record; subsequent calls are property reads.

## Troubleshooting

- **No enrichment locally** — expected; locally the enricher reports
  `ci.environment=local`. Set `GITHUB_SHA` etc. manually to force a specific
  shape for reproduction.
- **Missing PR number on push-event builds** — GitHub Actions only exposes
  `GITHUB_REF` as `refs/heads/<branch>` for push events; `pr_number` will be
  `null` and downstream enrichers must tolerate that.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- **Azure DevOps** uses `BUILD_BUILDID` / `SYSTEM_PULLREQUEST_PULLREQUESTID`
  — the enricher normalises to the GitHub shape.
- **Matrix jobs** — `MATRIX_CELL` is not a standard variable; the
  `ci.yml` workflow explicitly exports it as `MATRIX_CELL=<cell-name>` for
  each integration job so traces remain per-cell distinguishable.

## Benchmarks

See [`CiBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/CiBenchmarks.cs) and
the latest entries under `benchmarks/baseline-005.json`. No hot paths — this
is I/O-free property access.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)

## License

MIT. See [LICENSE](../../LICENSE).
