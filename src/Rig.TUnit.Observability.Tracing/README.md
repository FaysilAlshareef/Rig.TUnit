# Rig.TUnit.Observability.Tracing

> In-memory OpenTelemetry tracing fixture with `InMemoryExporter` and fluent `TraceAssert` for span / tag / status / parent-child verification.

## What this package is

The tracing counterpart to `Rig.TUnit.Observability.Logging`.
`TracingFixture` wires a `TracerProvider` with an `InMemoryExporter` so
every `Activity` emitted through a known `ActivitySource` gets captured.
`TraceAssert` is the fluent DSL: `.HasSpan(name).WithTag(…).WithStatus
(Ok).DurationLessThan(1s)`.

## When to use it

- Integration tests verifying spans are emitted at the right code sites.
- Asserting tag values and status codes on critical-path operations.
- Verifying parent/child relationships across async boundaries.
- **Not for**: E2E tests against a real OTLP collector.

## Prerequisites

- .NET 10 SDK
- `OpenTelemetry` + `.Exporter.InMemory` (transitive)

## Quick start

```csharp
using System.Diagnostics;
using Rig.TUnit.Observability.Tracing.Fixtures;
using Rig.TUnit.Observability.Tracing.Options;

await using var fx = new TracingFixture(new TracingFixtureOptions { ServiceName = "my-svc" });
await fx.InitializeAsync();

using (var act = fx.ActivitySource.StartActivity("op.work"))
{
    act?.SetStatus(ActivityStatusCode.Ok);
}
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `ServiceName` | `string` | `$"rigtunit-{IsolationKey}"` | OTEL resource attribute |
| `ActivitySourceName` | `string` | `"Rig.TUnit.Tests"` | Source name listened to |
| `RecordException` | `bool` | `true` | Auto-record exceptions as span events |

## Fixture + helper APIs

- `Rig.TUnit.Observability.Tracing.Fixtures.TracingFixture`
- `Rig.TUnit.Observability.Tracing.Options.TracingFixtureOptions`
- `Rig.TUnit.Observability.Tracing.Builder.TracingRigBuilder`
- `Rig.TUnit.Observability.Tracing.Assertions.TraceAssert`

## Per-test isolation

Each `TracingFixture` owns its exporter. `ServiceName` includes
`IsolationKey` so spans stay distinguishable across parallel fixtures.

## Parallelism + performance

- Fixture construction: ~3 ms.
- Per-span overhead: ~2 µs (no network).
- Safe under full parallelism.

## Troubleshooting

- **`HasSpan("name")` reports not-found** — `ActivitySource` name
  mismatch. Check `fx.ActivitySource.Name` matches what the code under
  test uses.
- **Parent-child broken across `Task.Run`** — `Activity.Current` is
  `AsyncLocal` and does cross `Task.Run`, but a thread-pool worker
  executed before the `Activity` started loses context. Instrument
  with explicit `ActivityContext` propagation.

See [docs/troubleshooting.md#tracing](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Spans are immutable after `Stop` — mutation attempts are silently
  ignored by OTEL SDK.
- `InMemoryExporter` keeps all spans until fixture disposal; long test
  runs with millions of spans can OOM. Call `fx.Clear()` between test
  phases if needed.
- Span status is `Unset` by default; promoting to `Ok` or `Error`
  requires an explicit `SetStatus` call.

## Benchmarks

See [`TracingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/TracingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Observability`](../Rig.TUnit.Observability/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
