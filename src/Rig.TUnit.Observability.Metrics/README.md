# Rig.TUnit.Observability.Metrics

> In-process `MeterListener`-based metrics capture with `MetricAssert` and `TagCardinalityGuard`.

## What this package is

A metrics testing fixture for `System.Diagnostics.Metrics`.
`MetricsFixture` creates a `MeterListener` wired to a named `Meter` —
your production code emits via the real `Meter.CreateCounter<T>` / etc.,
and the fixture records every sample for assertion. `TagCardinalityGuard`
is the novel piece: it asserts that a given tag's distinct-value count
stays within a budget, because unbounded cardinality is the #1 cause
of TSDB bill shock.

## When to use it

- Asserting counter / histogram / gauge increments match design
  expectations.
- Regression-guarding against cardinality explosions.
- Verifying tag-name consistency across instrumentation sites.
- **Not for**: E2E tests hitting a real Prometheus / OTLP collector —
  those need a separate integration flow.

## Prerequisites

- .NET 10 SDK
- `System.Diagnostics.DiagnosticSource` (in-box)

## Quick start

```csharp
using System.Diagnostics.Metrics;
using Rig.TUnit.Observability.Metrics.Fixtures;
using Rig.TUnit.Observability.Metrics.Options;

await using var fx = new MetricsFixture(new MetricsFixtureOptions
{
    MeterName = "orders.service",
});
await fx.InitializeAsync();

using var meter = new Meter("orders.service");
var counter = meter.CreateCounter<long>("orders.placed");
counter.Add(1);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `MeterName` | `string` | `"Rig.TUnit.Metrics"` | `Meter.Name` to listen on |
| `MaxTagCardinality` | `int` | `100` | Cardinality budget enforced by `TagCardinalityGuard` |
| `IncludeHistograms` | `bool` | `true` | Capture histogram samples |

## Fixture + helper APIs

- `Rig.TUnit.Observability.Metrics.Fixtures.MetricsFixture`
- `Rig.TUnit.Observability.Metrics.Options.MetricsFixtureOptions`
- `Rig.TUnit.Observability.Metrics.Builder.MetricsRigBuilder`
- `Rig.TUnit.Observability.Metrics.Assertions.MetricAssert`
- `Rig.TUnit.Observability.Metrics.Helpers.TagCardinalityGuard`

## Per-test isolation

Each fixture owns its `MeterListener`; listeners are per-fixture so
parallel tests don't cross-contaminate samples. `MeterName` includes
`IsolationKey` when using the default wiring.

## Parallelism + performance

- Fixture construction: ~1 ms.
- Per-sample capture: ~500 ns.
- Safe under full parallelism.

## Troubleshooting

- **`MetricAssert.Counter(…).Sum()` returns 0** — `MeterListener`
  started AFTER the `Meter` was created; ensure the fixture initialises
  before the code under test instantiates its meter.
- **Histograms return empty distribution** — set
  `IncludeHistograms=true` (default true) and confirm the histogram is
  actually recorded, not just created.

See [docs/troubleshooting.md#metrics](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `TagCardinalityGuard.EnsureWithinBudget` is a pure predicate — call
  it in test teardown or at assertion time, not in production code.
- Multiple meters can share a name; the fixture listens on ALL of
  them matching the `MeterName`.

## Benchmarks

See [`MetricsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MetricsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Observability`](../Rig.TUnit.Observability/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
