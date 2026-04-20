# Rig.TUnit.HealthChecks

> `HealthAssert` fluent assertions for ASP.NET Core health endpoints + `DependencyDownSimulator` for dependency-flip testing.

## What this package is

A small but opinionated health-check testing kit. `HealthAssert.On(
client, "/health/ready").Contains("database").IsHealthy().InTime(…)`
encodes the full assertion shape in one line. `DependencyDownSimulator`
lets a test mark a named dependency unhealthy and verify the
live/ready/startup probe reports correctly — the standard way to
catch probe mis-wirings before they cascade in production.

## When to use it

- Integration tests for services with `/health/live`, `/health/ready`,
  `/health/startup` endpoints.
- Verifying probe-response shape and timing.
- Asserting dependency-flip handling (DB down → ready unhealthy,
  live still up).
- **Not for**: unit-testing individual `IHealthCheck` implementations
  — use the ASP.NET Core `HealthCheckContext` directly.

## Prerequisites

- .NET 10 SDK
- Project under test registered health checks via
  `services.AddHealthChecks()` with named checks.

## Quick start

```csharp
using Rig.TUnit.HealthChecks;

await HealthAssert.On(client, "/health/ready")
    .Contains("database")
    .IsHealthy()
    .InTime(TimeSpan.FromSeconds(1));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultTimeout` | `TimeSpan` | `5s` | Per-probe call deadline |
| `ProbeKind` | `ProbeKind` | `Ready` | `Live` / `Ready` / `Startup` |
| `RequireJsonBody` | `bool` | `true` | Fail when response isn't JSON |

## Fixture + helper APIs

- `Rig.TUnit.HealthChecks.HealthAssert`
- `Rig.TUnit.HealthChecks.DependencyDownSimulator`
- `Rig.TUnit.HealthChecks.ProbeKind`

## Per-test isolation

Helpers are stateless. `DependencyDownSimulator` scopes its overrides
per-test via `IAsyncDisposable` — disposal restores normal health.
Safe under full parallelism.

## Parallelism + performance

- Per-probe call: ~5–20 ms (HTTP round-trip).
- Safe under full parallelism.

## Troubleshooting

- **`HealthAssert.Contains("database")` fails despite DB check
  registered** — the check's *name* must match. Registrations like
  `AddDbContextCheck<TDbContext>("database")` set the name; without it
  ASP.NET Core uses the class name.
- **`InTime(…)` fails intermittently** — the first probe call pays
  for JIT / connection-pool warm-up; run a warm-up call before the
  timed assertion.

See [docs/troubleshooting.md#healthchecks](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- ASP.NET Core's `/health` endpoint returns `Content-Type: text/plain`
  by default; set `ResponseWriter = UIResponseWriter.WriteHealthCheck
  UIResponse` (from `AspNetCore.HealthChecks.UI.Client`) for JSON,
  which `HealthAssert` expects.
- `ProbeKind.Startup` probes exist in Kubernetes contracts but not all
  apps define them — `HealthAssert` returns a documented no-op.

## Benchmarks

See [`HealthChecksBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/HealthChecksBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)

## License

MIT. See [LICENSE](../../LICENSE).
