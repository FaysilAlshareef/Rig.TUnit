# Rig.TUnit.Observability.Metrics

In-process metrics capture for Rig.TUnit. Wires a `MeterListener` around a named
`System.Diagnostics.Metrics.Meter` so production code emits through the real
Meter API while tests assert against the captured samples.

## Install

```bash
dotnet add package Rig.TUnit.Observability.Metrics
```

## Quick start

```csharp
using System.Diagnostics.Metrics;
using Rig.TUnit.Observability.Metrics.Assertions;
using Rig.TUnit.Observability.Metrics.Fixtures;
using Rig.TUnit.Observability.Metrics.Options;

await using var fx = new MetricsFixture(new MetricsFixtureOptions
{
    MeterName = "orders.service",
});
await fx.InitializeAsync();

using var meter = new Meter("orders.service");
var counter = meter.CreateCounter<long>("orders.placed");
counter.Add(1, KeyValuePair.Create<string, object?>("tenant", "acme"));

MetricAssert.Counter(fx.Capture, "orders.placed").Sum().Equals(1);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseMetricsCapture(RigConnect.FromValue("orders.service"), cfg => { })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `MeterName` | `Rig.TUnit.Metrics` | The `Meter.Name` to listen on |
| `MaxTagCardinality` | `100` | Cardinality budget enforced by `TagCardinalityGuard` |

Use `TagCardinalityGuard.EnsureWithinBudget(tagName, distinctCount, max)` as a
sanity-check helper for instrumentation that might otherwise blow a TSDB budget.
