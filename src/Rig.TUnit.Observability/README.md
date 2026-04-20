# Rig.TUnit.Observability

> Observability family-base: `ITelemetryRig`, `TelemetryFixtureBase`, `TelemetryRigBuilder<TSelf>` — shared shape with deterministic `IsolationKey` / `ServiceName`.

## What this package is

The shared contract for every Observability provider (`.AppInsights`,
`.Logging`, `.Logging.Analyzers`, `.Metrics`, `.Seq`, `.Tracing`).
Defines `ITelemetryRig`, `TelemetryFixtureBase`, and
`TelemetryRigBuilder<TSelf>` so every provider threads `IsolationKey`
into its `ServiceName` tag / resource attribute. Without this
consistent thread, traces and logs from parallel tests become
inscrutable.

Install one of the leaves directly for concrete testing.

## When to use it

- Authoring a new telemetry provider.
- Writing provider-agnostic observability helpers.
- **Not for**: concrete telemetry — install a leaf.

## Prerequisites

- .NET 10 SDK

## Quick start

```csharp
using Rig.TUnit.Observability.Fixtures;

public sealed class MyFixture : TelemetryFixtureBase
{
    public override string ConnectionString => string.Empty;
    public override Task InitializeAsync() => Task.CompletedTask;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string` | `$"rigtunit-{IsolationKey}"` | Resource attribute / activity source |
| `ServiceVersion` | `string` | `"0.0.0-test"` | OTEL resource version |
| `EnableAutoInstrumentation` | `bool` | `true` | Attach ASP.NET Core / HttpClient instrumentations |

## Fixture + helper APIs

- `Rig.TUnit.Observability.ITelemetryRig`
- `Rig.TUnit.Observability.Fixtures.TelemetryFixtureBase`
- `Rig.TUnit.Observability.Builder.TelemetryRigBuilder<TSelf>`

## Per-test isolation

`ServiceName` includes `IsolationKey` so every test's telemetry has a
unique provenance. Safe under full parallelism.

## Parallelism + performance

## §9 — N/A: family-base; per-provider cost. Logging/Tracing overhead
is a few µs/event; Seq involves network I/O.

## Troubleshooting

- **Trace context leaks across tests** — ensure `Activity.Current` is
  reset at test boundaries; the `TelemetryFixtureBase` disposer handles
  this for fixtures that derive from it.

See [docs/troubleshooting.md](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- OpenTelemetry is the common thread — Seq is an OTEL sink; Tracing is
  an OTEL processor; AppInsights is an OTEL exporter. Consistent
  resource attributes are essential.

## Benchmarks

## §12 — N/A: family-base; concrete leaves have individual
`*Benchmarks.cs` entries under `tests/Rig.TUnit.Benchmarks/`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Concrete: [`Rig.TUnit.Observability.Tracing`](../Rig.TUnit.Observability.Tracing/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
