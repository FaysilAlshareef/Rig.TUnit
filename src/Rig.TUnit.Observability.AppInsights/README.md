# Rig.TUnit.Observability.AppInsights

> In-process Application Insights fixture with `CapturingTelemetryChannel` — zero network egress, deterministic assertions.

## What this package is

A fixture that wires a real `TelemetryClient` to a
`CapturingTelemetryChannel` — the client API is production-identical
but the channel records every item into a thread-safe queue instead of
sending to Azure. Tests assert via `AppInsightsAssert.Event(…)`,
`.Dependency(…)`, `.Exception<T>(…)` with the familiar fluent shape.

## When to use it

- Integration tests for services using `Microsoft.ApplicationInsights`.
- Verifying custom events and dependency tracking fire at the right
  points.
- Asserting exception telemetry is emitted with correct properties.
- **Not for**: E2E tests against real Azure — those require a real
  instrumentation key and network access.

## Prerequisites

- .NET 10 SDK
- `Microsoft.ApplicationInsights` 2.x (transitive)

## Quick start

```csharp
using Rig.TUnit.Observability.AppInsights.Fixtures;

await using var fx = new AppInsightsFixture();
await fx.InitializeAsync();

fx.Client.TrackEvent("order.placed");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `InstrumentationKey` | `string` | `"00000000-0000-0000-0000-000000000000"` | Placeholder — no egress |
| `RoleName` | `string` | `"rigtunit-tests"` | `cloud_RoleName` on every item |
| `EnableLiveMetrics` | `bool` | `false` | Off — Live Metrics is an egress path |

## Fixture + helper APIs

- `Rig.TUnit.Observability.AppInsights.Fixtures.AppInsightsFixture`
- `Rig.TUnit.Observability.AppInsights.Options.AppInsightsFixtureOptions`
- `Rig.TUnit.Observability.AppInsights.Builder.AppInsightsRigBuilder`
- `Rig.TUnit.Observability.AppInsights.Channels.CapturingTelemetryChannel`
- `Rig.TUnit.Observability.AppInsights.Assertions.AppInsightsAssert`

## Per-test isolation

Each `AppInsightsFixture` owns its channel; captured items are never
cross-test visible. Safe under full parallelism.

## Parallelism + performance

- Fixture construction: ~2 ms.
- `Track*` call overhead: ~5 µs (no network).
- Safe under full parallelism.

## Troubleshooting

- **Telemetry not captured** — ensure you are using `fx.Client`, not a
  `TelemetryClient` constructed elsewhere; only the fixture's client is
  wired to the capturing channel.
- **`AppInsightsAssert.Event(…).Exactly(1)` fails with 0** — some
  telemetry is batched by the SDK; call `fx.Client.Flush()` before
  asserting.

See [docs/troubleshooting.md#appinsights](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- The capturing channel stores items in a `ConcurrentQueue` — the
  assertion API LINQs over the snapshot, so very fast producers can
  observe a count mid-flight; always `Flush()` first.
- `RoleName` is set globally; parallel fixtures with different role
  names must not share a static `TelemetryConfiguration`.

## Benchmarks

See [`AppInsightsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/AppInsightsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Observability`](../Rig.TUnit.Observability/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
